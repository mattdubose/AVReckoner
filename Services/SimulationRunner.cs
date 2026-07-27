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
                results.Add(new SimulationDayResult
                {
                    Date = date,
                    Action = accountService.LastAction,
                    Balance = balance,
                    Cash = cash,
                    Contribution = accountService.LastContribution,
                });
            }
            return results;
        }
    }
}
