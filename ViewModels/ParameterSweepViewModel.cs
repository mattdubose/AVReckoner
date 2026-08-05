using Avalonia.Controls;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
    // One named template to sweep from — just enough of an AnalystScenarioSlot's settings to
    // display in the "Template Scenario" picker and to clone from.
    public partial class ParameterSweepViewModel : BaseViewModel
    {
        // A hard ceiling on how many scenarios one sweep can generate — protects against an
        // analyst accidentally queuing up a multi-hour run (e.g. all 4 knobs at 10+ steps each).
        private const int MaxCombinations = 200;

        private readonly IReadOnlyList<AnalystScenarioSlot> _templates;
        private readonly Func<string, AnalystScenarioSlot> _createSlot;

        public List<AnalystScenarioSlot> Templates => _templates.ToList();
        [ObservableProperty] private AnalystScenarioSlot? selectedTemplate;

        [ObservableProperty] private ObservableCollection<SweepKnob> knobs = new();

        [ObservableProperty] private ObservableCollection<AnalystResultRow> results = new();
        [ObservableProperty] private bool isRunning = false;
        [ObservableProperty] private string runError = string.Empty;
        [ObservableProperty] private bool hasRunError;

        [ObservableProperty] private ObservableCollection<EpisodeRow> events = new();
        [ObservableProperty] private EpisodeRow? selectedEvent;

        [ObservableProperty] private ISeries[] detailSeries = Array.Empty<ISeries>();
        [ObservableProperty] private RectangularSection[] detailSections = Array.Empty<RectangularSection>();
        [ObservableProperty] private Axis[] detailXAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM yyyy"))
            {
                MinStep = TimeSpan.FromDays(28).Ticks,
                LabelsRotation = -45,
                TextSize = 11,
            }
        };

        [ObservableProperty] private Axis[] detailYAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => value.ToString("C2"),
                TextSize = 11,
            }
        };

        public ParameterSweepViewModel(AppShellService appShell, IReadOnlyList<AnalystScenarioSlot> templates,
            Func<string, AnalystScenarioSlot> createSlot) : base(appShell)
        {
            _templates = templates;
            _createSlot = createSlot;
            SelectedTemplate = _templates.FirstOrDefault();
        }

        partial void OnSelectedTemplateChanged(AnalystScenarioSlot? value)
        {
            Knobs = value != null
                ? SweepKnob.BuildDefaults(value.SimSettingsVM.StrategySettings)
                : new ObservableCollection<SweepKnob>();
        }

        private bool CanRunSweep() => SelectedTemplate != null && !IsRunning;

        [RelayCommand(CanExecute = nameof(CanRunSweep))]
        private async Task RunSweep()
        {
            var template = SelectedTemplate;
            if (template == null) return;

            var enabledKnobs = Knobs.Where(k => k.IsEnabled).ToList();
            if (enabledKnobs.Count == 0)
            {
                RunError = "Check at least one parameter to sweep.";
                HasRunError = true;
                return;
            }

            var combinations = BuildCombinations(enabledKnobs);
            if (combinations.Count == 0)
            {
                RunError = "That range/step combination produces no scenarios — check Min/Max/Step.";
                HasRunError = true;
                return;
            }
            if (combinations.Count > MaxCombinations)
            {
                RunError = $"That's {combinations.Count} scenarios — narrow the range or widen the step (limit is {MaxCombinations}).";
                HasRunError = true;
                return;
            }

            IsRunning = true;
            RunError = string.Empty;
            HasRunError = false;
            DetailSeries = Array.Empty<ISeries>();
            DetailSections = Array.Empty<RectangularSection>();
            Events.Clear();

            var newResults = new List<AnalystResultRow>();

            foreach (var combo in combinations)
            {
                string name = string.Join(", ", combo.Select(c => $"{c.Knob.Label}: {c.Knob.FormatModelValue(c.Value)}"));
                var slot = _createSlot(name);
                CopyBaseSettings(template.SimSettingsVM.ActiveSimSettings, slot.SimSettingsVM.ActiveSimSettings);
                slot.SimSettingsVM.ActiveSimSettings.Strategy = InvestmentStrategy.FWStrategy;
                slot.SimSettingsVM.StrategySettings = template.SimSettingsVM.StrategySettings.Clone();
                foreach (var c in combo)
                    SweepKnob.Apply(slot.SimSettingsVM.StrategySettings, c.Knob.Parameter, c.Value);
                slot.SimSettingsVM.SelectedFwTicker = slot.SimSettingsVM.ActiveSimSettings.Holdings
                    .FirstOrDefault(h => h.TickerSymbol == template.SimSettingsVM.SelectedFwTicker?.TickerSymbol);

                try
                {
                    var row = await Task.Run(() =>
                    {
                        slot.SimSettingsVM.SetSelections();
                        DateTime start = slot.SimSettingsVM.ActiveSimSettings.StartDateOffset?.DateTime ?? DateTime.Now;
                        DateTime end = slot.SimSettingsVM.ActiveSimSettings.EndDateOffset?.DateTime ?? DateTime.Now;
                        var days = SimulationRunner.Run(slot.AccountService, start, end);
                        var episodes = new List<StrategyEpisode>(slot.AccountService.CompletedEpisodes);
                        if (slot.AccountService.CurrentEpisode != null)
                            episodes.Add(slot.AccountService.CurrentEpisode);
                        return new AnalystResultRow
                        {
                            Name = name,
                            EndBalance = days.Count > 0 ? days[^1].Balance : 0,
                            Days = days,
                            ReferenceTicker = slot.SimSettingsVM.SelectedFwTicker?.TickerSymbol,
                            HoldingTickers = slot.SimSettingsVM.ActiveSimSettings.Holdings
                                .Select(h => h.TickerSymbol).Distinct().ToList(),
                            Episodes = episodes,
                        };
                    });
                    newResults.Add(row);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Sweep scenario '{name}' failed: {ex.Message}");
                }
            }

            Results = new ObservableCollection<AnalystResultRow>(newResults.OrderByDescending(r => r.EndBalance));
            if (Results.Count == 0)
            {
                RunError = "No sweep scenario completed successfully.";
                HasRunError = true;
            }
            IsRunning = false;
        }

        private static void CopyBaseSettings(SimulationSettings from, SimulationSettings to)
        {
            to.Holdings.Clear();
            foreach (var h in from.Holdings)
                to.Holdings.Add(new SecurityHolding(h));
            to.ContributionHelper = from.ContributionHelper;
            to.StartDateOffset = from.StartDateOffset;
            to.EndDateOffset = from.EndDateOffset;
            to.ContributionAmount = from.ContributionAmount;
            to.SelectedDayOfWeek = from.SelectedDayOfWeek;
            to.ContributionDates = from.ContributionDates;
            to.ContributionInterval = from.ContributionInterval;
            to.RebalanceInterval = from.RebalanceInterval;
            to.DividendReinvestment = from.DividendReinvestment;
            to.InitialCash = from.InitialCash;
        }

        // Cartesian product of every enabled knob's values — e.g. 2 knobs x 5 steps each = 25
        // combinations, each carrying the knob it came from (for naming/formatting later).
        private static List<(SweepKnob Knob, decimal Value)[]> BuildCombinations(List<SweepKnob> enabledKnobs)
        {
            IEnumerable<(SweepKnob, decimal)[]> combos = new List<(SweepKnob, decimal)[]> { Array.Empty<(SweepKnob, decimal)>() };
            foreach (var knob in enabledKnobs)
            {
                var values = knob.ModelValues().ToList();
                combos = combos.SelectMany(prefix => values.Select(v => prefix.Append((knob, v)).ToArray()));
            }
            return combos.ToList();
        }

        public void UpdateSelectedResults(IReadOnlyList<AnalystResultRow> selected)
        {
            if (selected.Count == 0)
            {
                DetailSeries = Array.Empty<ISeries>();
                DetailSections = Array.Empty<RectangularSection>();
                Events.Clear();
                return;
            }

            DetailSeries = ComparisonChartHelper.BuildSeries(
                selected.Select(row => (row.Name, row.Days)).ToList());
            DetailSections = ComparisonChartHelper.BuildSections(
                selected.Select(row => (row.Name, row.Days, row.Episodes)).ToList());

            Events = new ObservableCollection<EpisodeRow>(
                selected
                    .SelectMany(row => row.Episodes.Select(ep => new EpisodeRow { ScenarioName = row.Name, Episode = ep }))
                    .OrderBy(e => e.StartDate));
        }

        [RelayCommand]
        private void ZoomToEvent(EpisodeRow evt)
        {
            if (evt == null) return;
            DetailXAxes = new Axis[]
            {
                new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d, yyyy"))
                {
                    MinStep = TimeSpan.FromDays(1).Ticks,
                    LabelsRotation = -45,
                    TextSize = 11,
                    MinLimit = evt.StartDate.AddDays(-60).Ticks,
                    MaxLimit = evt.StartDate.AddDays(60).Ticks,
                }
            };
        }

        [RelayCommand]
        private void ResetZoom()
        {
            DetailXAxes = new Axis[]
            {
                new DateTimeAxis(TimeSpan.FromDays(1), date =>
                    date.Month == 1 && date.Day <= 7 ? date.ToString("yyyy") : date.ToString("MMM yyyy"))
                {
                    MinStep = TimeSpan.FromDays(28).Ticks,
                    LabelsRotation = -45,
                    TextSize = 11,
                }
            };
        }

        partial void OnSelectedEventChanged(EpisodeRow? value)
        {
            if (value != null) ZoomToEvent(value);
        }

        // Opens the detail popup for a double-clicked event row — called from code-behind since
        // DataGrid doesn't have a built-in double-click-to-command binding.
        public async Task ShowEpisodeDetail(EpisodeRow row, Window owner)
        {
            var win = new Views.EpisodeDetailWindow
            {
                DataContext = new EpisodeDetailViewModel(row.Episode)
            };
            await win.ShowDialog(owner);
        }
    }
}
