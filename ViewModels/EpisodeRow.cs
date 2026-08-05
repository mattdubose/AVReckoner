namespace Reckoner.ViewModels
{
    // One row per StrategyEpisode in the Buy/Sell Events grid — a compact summary; the full
    // detail (reasons, trigger tickers, holdings before/after) shows in EpisodeDetailWindow on
    // double-click instead of being spread across grid columns.
    public class EpisodeRow
    {
        public string ScenarioName { get; set; } = string.Empty;
        public StrategyEpisode Episode { get; set; } = null!;

        public DateTime StartDate => Episode.StartDate;
        public DateTime? EndDate => Episode.EndDate;
        public string EntryReasonCode => Episode.EntryReason?.Code ?? string.Empty;
        public string ExitReasonCode => Episode.ExitReason?.Code ?? "(ongoing)";
    }
}
