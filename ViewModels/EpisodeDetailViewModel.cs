namespace Reckoner.ViewModels
{
    // Read-only display wrapper for a StrategyEpisode, shown in EpisodeDetailWindow. Keeps
    // UI-formatting concerns (date-range text, "no reason recorded" fallbacks) out of the model.
    public class EpisodeDetailViewModel
    {
        public string DateRange { get; }
        public string EntryReasonText { get; }
        public string ExitReasonText { get; }
        public List<TickerSnapshot> EntryTriggerTickers { get; }
        public List<TickerSnapshot> ExitTriggerTickers { get; }
        public List<TickerSnapshot> HoldingsAtStart { get; }
        public List<TickerSnapshot> HoldingsAtEnd { get; }

        public EpisodeDetailViewModel(StrategyEpisode episode)
        {
            DateRange = episode.EndDate.HasValue
                ? $"{episode.StartDate:MM/dd/yyyy} → {episode.EndDate.Value:MM/dd/yyyy}"
                : $"{episode.StartDate:MM/dd/yyyy} → ongoing";
            EntryReasonText = episode.EntryReason != null
                ? $"[{episode.EntryReason.Code}] {episode.EntryReason.Description}"
                : "(no reason recorded)";
            ExitReasonText = episode.ExitReason != null
                ? $"[{episode.ExitReason.Code}] {episode.ExitReason.Description}"
                : "(still open)";
            EntryTriggerTickers = episode.EntryReason?.TriggerTickers ?? new();
            ExitTriggerTickers = episode.ExitReason?.TriggerTickers ?? new();
            HoldingsAtStart = episode.HoldingsAtStart;
            HoldingsAtEnd = episode.HoldingsAtEnd;
        }
    }
}
