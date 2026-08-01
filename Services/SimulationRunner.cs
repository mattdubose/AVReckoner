using Reckoner.Models;
using Reckoner.Utilities;
using System;
using System.Collections.Generic;

namespace Reckoner.Services
{
    /// Runs a simulation day-by-day with no chart/animation/pause plumbing — for batch/analysis
    /// tooling that only cares about the resulting numbers, not watching it draw.
    public static class SimulationRunner
    {
        public static List<SimulationDayResult> Run(AccountService accountService, DateTime start, DateTime end)
        {
            var managedTimeProvider = new ManagedDateTime();
            DateTimeService.GetInstance.SetDateProvider(managedTimeProvider);
            accountService.PreloadForSimulation(start, end);

            var results = new List<SimulationDayResult>();
            for (var date = start; date < end; date = date.AddDays(1))
            {
                managedTimeProvider.SetCurrentDate(date);
                accountService.RunDaysActivities();
                decimal cash = accountService.GetAccount().CashBalance;
                decimal balance = accountService.GetBalance() + cash;

                // Every held ticker's price, every day — cheap (prices are already cached from
                // Preload) and lets callers build a "what happened on this date" view without a
                // second pass over the date range.
                var closes = new Dictionary<string, decimal>();
                var holdingShares = new Dictionary<string, decimal>();
                foreach (var asset in accountService.Assets)
                {
                    closes[asset.TickerSymbol] = asset.GetLatestPrice();
                    holdingShares[asset.TickerSymbol] = asset.NumberOfShares;
                }

                results.Add(new SimulationDayResult
                {
                    Date = date,
                    Action = accountService.LastAction,
                    Balance = balance,
                    Cash = cash,
                    Contribution = accountService.LastContribution,
                    Closes = closes,
                    HoldingShares = holdingShares,
                    HoldingPrices = closes,
                    ReferenceHighs = new Dictionary<string, decimal>(accountService.InvestmentStrategyService.EvaluationHighs),
                });
            }
            return results;
        }
    }
}
