using System;
using System.Collections.Generic;

namespace Reckoner.Models
{
    /// A ticker's price/quantity at a point in time. Reused for two different sets: a portfolio
    /// snapshot (Shares always populated, cash included as Ticker == AccountService.CashTicker
    /// with Price == 1) and a reason's TriggerTickers (Shares null when the ticker is referenced
    /// only as a signal, not actually held).
    public class TickerSnapshot
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? Shares { get; set; }
        public decimal Value => Price * (Shares ?? 0);
    }

    /// Why a strategy entered or exited an episode — strategy-supplied, since the vocabulary of
    /// reasons (price threshold, time elapsed, recovery, ...) varies by strategy.
    public class EpisodeReason
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TickerSnapshot> TriggerTickers { get; set; } = new();
    }

    /// One "out of the market" episode for a strategy: the sell-off date, the eventual buy-back
    /// date (null while still ongoing), why each happened, and what the portfolio looked like at
    /// both ends — so an analyst (or a programmer debugging a run) can inspect a shaded chart
    /// region without having to re-derive it from raw day-by-day Action history.
    public class StrategyEpisode
    {
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<TickerSnapshot> HoldingsAtStart { get; set; } = new();
        public List<TickerSnapshot> HoldingsAtEnd { get; set; } = new();
        public EpisodeReason? EntryReason { get; set; }
        public EpisodeReason? ExitReason { get; set; }
    }
}
