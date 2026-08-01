using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Reckoner.Repositories;
using SkiaSharp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
    // One row in the scenario queue: its own independent Account/AccountService/settings,
    // so scenarios can differ by ticker, strategy, parameters, or holdings without interfering
    // with each other or with Client View.
    public partial class AnalystScenarioSlot : ObservableObject
    {
        public AccountService AccountService { get; }
        public SimulationSettingsViewModel SimSettingsVM { get; }

        [ObservableProperty] private string statusMessage = string.Empty;

        public AnalystScenarioSlot(AccountService accountService, SimulationSettingsViewModel simSettingsVM)
        {
            AccountService = accountService;
            SimSettingsVM = simSettingsVM;
        }
    }

    // One completed run, ready to rank/compare.
    public partial class AnalystResultRow : ObservableObject
    {
        public string Name { get; set; } = string.Empty;
        public decimal EndBalance { get; set; }
        public List<SimulationDayResult> Days { get; set; } = new();
        public string? ReferenceTicker { get; set; }
        /// This scenario's actual holdings — drives the Shares/Price/Reference-High columns on export.
        public List<string> HoldingTickers { get; set; } = new();
    }

    // A Buy/Sell transition day, with every held ticker's price that day — the "what actually
    // happened on this date" detail the chart alone can't show at a glance.
    public partial class AnalystChartEvent : ObservableObject
    {
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string EventType { get; set; } = string.Empty; // "Sold" or "Bought Back"
        public string? ReferenceTicker { get; set; }
        public decimal? ReferencePrice { get; set; }
        public string AllPrices { get; set; } = string.Empty;
    }

    public partial class AnalystViewModel : BaseViewModel
    {
        private readonly AppStateService _appState;
        private readonly IClientRepository _clientRepo;
        private readonly IAccountRepository _accountRepo;

        // Which client/account the scenario queue is built from — Analyst View has its own
        // picker rather than requiring a detour through the Clients page first.
        public ObservableCollection<Client> Clients { get; } = new();
        public ObservableCollection<Account> Accounts { get; } = new();
        [ObservableProperty] private Client? selectedClient;
        [ObservableProperty] private Account? selectedAccount;
        public bool AccountPickerVisible => SelectedClient != null;

        [ObservableProperty] private ObservableCollection<AnalystScenarioSlot> scenarios = new();
        public bool HasNoScenarios => Scenarios.Count == 0;
        [ObservableProperty] private ObservableCollection<AnalystResultRow> results = new();
        [ObservableProperty] private bool isRunning = false;
        [ObservableProperty] private string runError = string.Empty;
        [ObservableProperty] private bool hasRunError;

        [ObservableProperty] private ObservableCollection<AnalystChartEvent> events = new();
        [ObservableProperty] private AnalystChartEvent? selectedEvent;

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

        // Without an explicit Labeler, LiveCharts prints the raw double (decimal->double
        // conversion noise included) on both the axis and the hover tooltip — force it to
        // money with 2 decimal places everywhere it shows a value.
        [ObservableProperty] private Axis[] detailYAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => value.ToString("C2"),
                TextSize = 11,
            }
        };

        public AnalystViewModel(AppShellService appShell, AppStateService appState,
            IClientRepository clientRepo, IAccountRepository accountRepo) : base(appShell)
        {
            _appState = appState;
            _clientRepo = clientRepo;
            _accountRepo = accountRepo;

            foreach (var client in _clientRepo.GetAllClients())
                Clients.Add(client);

            // If a client/account is already active elsewhere in the app, start from that —
            // otherwise the queue stays empty until one is picked below.
            if (_appState.CurrentClient != null)
                SelectedClient = Clients.FirstOrDefault(c => c.ClientId == _appState.CurrentClient.ClientId);
        }

        partial void OnSelectedClientChanged(Client? value)
        {
            Accounts.Clear();
            SelectedAccount = null;
            OnPropertyChanged(nameof(AccountPickerVisible));
            if (value == null) return;

            var accounts = _accountRepo.GetAccountsByClientIDAsync(value.ClientId).Result;
            foreach (var account in accounts)
                Accounts.Add(account);

            if (_appState.CurrentAccount != null)
                SelectedAccount = Accounts.FirstOrDefault(a => a.AccountId == _appState.CurrentAccount.AccountId);
        }

        partial void OnSelectedAccountChanged(Account? value)
        {
            if (value == null) return;
            _appState.CurrentClient = SelectedClient;
            _appState.CurrentAccount = value;

            // Existing scenario cards were built against the previous (possibly empty) account —
            // rebuild fresh rather than leave stale holdings sitting around.
            Scenarios.Clear();
            Results.Clear();
            DetailSeries = Array.Empty<ISeries>();
            AddScenario();
            AddScenario();
        }

        [RelayCommand]
        private void AddScenario()
        {
            var slot = CreateScenarioSlot($"Scenario #{Scenarios.Count + 1}");
            Scenarios.Add(slot);
            OnPropertyChanged(nameof(HasNoScenarios));
            RunAllCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void RemoveScenario(AnalystScenarioSlot slot)
        {
            if (slot == null) return;
            Scenarios.Remove(slot);
            OnPropertyChanged(nameof(HasNoScenarios));
            RunAllCommand.NotifyCanExecuteChanged();
        }

        private AnalystScenarioSlot CreateScenarioSlot(string name)
        {
            Account account = _appState.CurrentAccount != null
                ? Account.DeepCopy(_appState.CurrentAccount)
                : new Account();
            var assetServices = SLMarketSecurityHelper.BuildAssetServices(account);
            var accountService = new AccountService(account, assetServices);
            var simSettingsVM = new SimulationSettingsViewModel(_appShellService, accountService);

            // SimulationSettingsViewModel's own constructor inherits the persisted account's
            // real-world contribution schedule (e.g. a start date from years before this demo's
            // price history begins). Client View avoids that by building each scenario tab's
            // settings fresh instead of using the inherited ones — do the same here.
            var settings = new SimulationSettings(_appShellService) { Name = name };
            foreach (var asset in accountService.Assets)
                settings.Holdings.Add(new SecurityHolding(asset));
            simSettingsVM.ActiveSimSettings = settings;

            return new AnalystScenarioSlot(accountService, simSettingsVM);
        }

        private bool CanRunAll() => Scenarios.Count > 0 && !IsRunning;

        partial void OnIsRunningChanged(bool value)
        {
            RunAllCommand.NotifyCanExecuteChanged();
            ExportToExcelCommand.NotifyCanExecuteChanged();
        }

        partial void OnResultsChanged(ObservableCollection<AnalystResultRow> value) => ExportToExcelCommand.NotifyCanExecuteChanged();

        private bool CanExportToExcel() => Results.Count > 0 && !IsRunning;

        [RelayCommand(CanExecute = nameof(CanExportToExcel))]
        private async Task ExportToExcel(TopLevel root)
        {
            if (root is not Window owner) return;

            var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Scenario Results",
                SuggestedFileName = $"AnalystScenarios_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExtension = "xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } }
                }
            });
            if (file == null) return;

            try
            {
                var runs = Results.Select(r => new SimulationRunExport
                {
                    Name = r.Name,
                    Days = r.Days,
                    HoldingTickers = r.HoldingTickers,
                }).ToList();
                await Task.Run(() => ExcelExportService.ExportRuns(file.Path.LocalPath, runs));
            }
            catch (Exception ex)
            {
                RunError = $"Excel export failed: {ex.Message}";
                HasRunError = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanRunAll))]
        private async Task RunAll()
        {
            IsRunning = true;
            RunError = string.Empty;
            HasRunError = false;
            DetailSeries = Array.Empty<ISeries>();

            var newResults = new List<AnalystResultRow>();

            foreach (var slot in Scenarios.ToList())
            {
                slot.StatusMessage = "Running...";
                try
                {
                    var row = await Task.Run(() =>
                    {
                        slot.SimSettingsVM.SetSelections();
                        DateTime start = slot.SimSettingsVM.ActiveSimSettings.StartDateOffset?.DateTime ?? DateTime.Now;
                        DateTime end = slot.SimSettingsVM.ActiveSimSettings.EndDateOffset?.DateTime ?? DateTime.Now;
                        var days = SimulationRunner.Run(slot.AccountService, start, end);
                        return new AnalystResultRow
                        {
                            Name = slot.SimSettingsVM.ActiveSimSettings.Name,
                            EndBalance = days.Count > 0 ? days[^1].Balance : 0,
                            Days = days,
                            ReferenceTicker = slot.SimSettingsVM.SelectedFwTicker?.TickerSymbol,
                            HoldingTickers = slot.SimSettingsVM.ActiveSimSettings.Holdings
                                .Select(h => h.TickerSymbol).Distinct().ToList(),
                        };
                    });
                    newResults.Add(row);
                    slot.StatusMessage = "Done";
                }
                catch (Exception ex)
                {
                    slot.StatusMessage = $"Error: {ex.Message}";
                }
            }

            Results = new ObservableCollection<AnalystResultRow>(newResults.OrderByDescending(r => r.EndBalance));
            if (Results.Count == 0 && Scenarios.Count > 0)
            {
                RunError = "No scenario completed successfully — check each scenario's status message.";
                HasRunError = true;
            }
            IsRunning = false;
        }

        private static readonly SKColor[] _detailPalette =
        {
            SKColors.Black, SKColors.Green, SKColors.Blue, SKColors.Magenta, SKColors.Red,
            SKColors.DarkOrange, SKColors.Teal, SKColors.Purple,
        };

        // Called from the results grid's SelectionChanged (code-behind) — one line per
        // selected scenario, so you can compare several at once instead of just the top pick.
        // Sell-off stretches (Action == Sell, i.e. holding cash out of the market) get a
        // translucent red background band instead of recoloring the line itself — a second
        // line series per segment made the tooltip show multiple disconnected entries for what
        // was supposed to be one continuous scenario, which was more confusing than the zero dip
        // it replaced.
        public void UpdateSelectedResults(IReadOnlyList<AnalystResultRow> selected)
        {
            if (selected.Count == 0)
            {
                DetailSeries = Array.Empty<ISeries>();
                DetailSections = Array.Empty<RectangularSection>();
                Events.Clear();
                return;
            }

            DetailSeries = selected.Select((row, i) => (ISeries)new LineSeries<DateTimePoint>
            {
                Values = row.Days.Select(d => new DateTimePoint(d.Date, (double)d.Balance)).ToList(),
                Stroke = new SolidColorPaint(_detailPalette[i % _detailPalette.Length]) { StrokeThickness = 2 },
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Name = row.Name,
            }).ToArray();

            // Zoomed out over a long backtest, a short sell-off can be only a few pixels wide —
            // pad it out to a visible minimum width and use a stronger fill so it still reads as
            // "eye popping" rather than disappearing at the current zoom level.
            DetailSections = selected
                .SelectMany(row => GetSellOffRanges(row.Days))
                .Select(range =>
                {
                    var padded = range.end - range.start < TimeSpan.FromDays(10)
                        ? (range.start.AddDays(-5), range.end.AddDays(5))
                        : range;
                    return new RectangularSection
                    {
                        Xi = padded.Item1.Ticks,
                        Xj = padded.Item2.Ticks,
                        Fill = new SolidColorPaint(SKColors.Red.WithAlpha(110)),
                    };
                })
                .ToArray();

            Events = new ObservableCollection<AnalystChartEvent>(
                selected.SelectMany(row => BuildEvents(row))
                    .OrderBy(e => e.Date));
        }

        // One row per Buy/Sell transition — the exact date, and every held ticker's price that
        // day, so you don't have to pixel-hunt on the chart to see what actually triggered it.
        private static IEnumerable<AnalystChartEvent> BuildEvents(AnalystResultRow row)
        {
            for (int i = 0; i < row.Days.Count; i++)
            {
                bool isSellOff = row.Days[i].Action == SuggestedAction.Sell;
                bool wasSellOff = i > 0 && row.Days[i - 1].Action == SuggestedAction.Sell;
                if (isSellOff == wasSellOff) continue;

                var day = row.Days[i];
                decimal? referencePrice = row.ReferenceTicker != null && day.Closes.TryGetValue(row.ReferenceTicker, out var p)
                    ? p
                    : null;
                yield return new AnalystChartEvent
                {
                    ScenarioName = row.Name,
                    Date = day.Date,
                    EventType = isSellOff ? "Sold" : "Bought Back",
                    ReferenceTicker = row.ReferenceTicker,
                    ReferencePrice = referencePrice,
                    AllPrices = string.Join(", ", day.Closes.Select(kv =>
                        $"{kv.Key}{(kv.Key == row.ReferenceTicker ? "★" : "")}: {kv.Value:C2}")),
                };
            }
        }

        [RelayCommand]
        private void ZoomToEvent(AnalystChartEvent evt)
        {
            if (evt == null) return;
            DetailXAxes = new Axis[]
            {
                new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMM d, yyyy"))
                {
                    MinStep = TimeSpan.FromDays(1).Ticks,
                    LabelsRotation = -45,
                    TextSize = 11,
                    MinLimit = evt.Date.AddDays(-60).Ticks,
                    MaxLimit = evt.Date.AddDays(60).Ticks,
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

        partial void OnSelectedEventChanged(AnalystChartEvent? value)
        {
            if (value != null) ZoomToEvent(value);
        }

        // Contiguous (start, end) date ranges where the day's Action was Sell.
        private static IEnumerable<(DateTime start, DateTime end)> GetSellOffRanges(List<SimulationDayResult> days)
        {
            DateTime? rangeStart = null;
            for (int i = 0; i < days.Count; i++)
            {
                bool isSellOff = days[i].Action == SuggestedAction.Sell;
                if (isSellOff && rangeStart == null)
                {
                    rangeStart = days[i].Date;
                }
                else if (!isSellOff && rangeStart != null)
                {
                    yield return (rangeStart.Value, days[i - 1].Date);
                    rangeStart = null;
                }
            }
            if (rangeStart != null) yield return (rangeStart.Value, days[^1].Date);
        }
    }
}
