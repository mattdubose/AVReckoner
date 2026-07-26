using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using Reckoner.Repositories;
using Reckoner.Utilities;
using System.Threading.Tasks;
using System.ComponentModel.Design.Serialization;

namespace Reckoner.ViewModels
{

    public partial class InvestmentPerformanceViewModel : BaseViewModel
    {
        IUiThreadDispatcher _dispatcher;
    
        [ObservableProperty]
        SimulationSettingsViewModel simSettingsVM;

        partial void OnSimSettingsVMChanged(SimulationSettingsViewModel oldValue, SimulationSettingsViewModel newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SimSettingsVM_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += SimSettingsVM_PropertyChanged;

            TogglePlayPauseCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private bool simulationHasResults = false;

        [ObservableProperty]
        private string simulationError = string.Empty;
        [ObservableProperty]
        private bool hasSimulationError;

        private void SimSettingsVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Any setting change invalidates the current results
            SimulationHasResults = false;
            SetSimulationError(string.Empty);
            TogglePlayPauseCommand.NotifyCanExecuteChanged();
        }

        private void SetSimulationError(string message)
        {
            SimulationError = message;
            HasSimulationError = !string.IsNullOrEmpty(message);
        }

        private bool CanTogglePlayPause() => SimSettingsVM?.IsValid == true && !SimulationHasResults;

        private Account _myAccount = new Account();
        Dictionary<int, SimulationSettings> _simSettings;
        private readonly Dictionary<int, List<SimulationDayResult>> _simDayResults = new();
        bool _quitSimulation = false;
        [ObservableProperty]
        bool showSearchView = false;

        [RelayCommand]
        private void AddNewHolding()
        {
            ShowSearchView = true;
        }

        [RelayCommand]
        private void CancelAddHolding()
        {
            ShowSearchView = false;
            StockSearchVM.SearchText = string.Empty;
        }

        [RelayCommand] void CancelSim() { _quitSimulation = true; IsPaused = true; }

        [RelayCommand]
        async Task ClearSims()
        {
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                foreach (var series in ListOfLines.OfType<LineSeries<DateTimePoint>>())
                {
                    series.Values = new List<DateTimePoint>();
                }
            });
            _numLinesUsed = 0;
            _simDayResults.Clear();
            SimulationHasResults = false;
            TogglePlayPauseCommand.NotifyCanExecuteChanged();
            ExportToExcelCommand.NotifyCanExecuteChanged();
        }

        // Exporting re-reads market prices via the shared DateTimeService date provider (see
        // BuildExportRuns) — blocked while a simulation is running to avoid both fighting over it.
        private bool CanExportToExcel() => _simDayResults.Count > 0 && !IsSimulationRunning;

        [RelayCommand(CanExecute = nameof(CanExportToExcel))]
        private async Task ExportToExcel(TopLevel root)
        {
            if (root is not Window owner) return;

            var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Simulation Results",
                SuggestedFileName = $"InvestmentSimulation_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExtension = "xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } }
                }
            });
            if (file == null) return;

            try
            {
                var runs = await Task.Run(BuildExportRuns);
                await Task.Run(() => ExcelExportService.ExportRuns(file.Path.LocalPath, runs));
            }
            catch (Exception ex)
            {
                SetSimulationError($"Excel export failed: {ex.Message}");
            }
        }

        // Fills in Close/All-Time-High for whichever tickers are checked "Track in Export" right
        // now — across every scenario tab, not just the one active when each run happened — so
        // checking a box after a run has already finished still shows up in the export.
        private List<SimulationRunExport> BuildExportRuns()
        {
            var trackedTickers = _simSettings.Values
                .SelectMany(s => s.Holdings)
                .Where(h => h.TrackInExport)
                .Select(h => h.TickerSymbol)
                .Distinct()
                .ToList();

            List<AssetService> priceAssets = new();
            if (trackedTickers.Count > 0)
            {
                var priceLookupAccount = new Account();
                foreach (var ticker in trackedTickers)
                    priceLookupAccount.Assets.Add(new SecurityHolding(ticker, ticker));
                priceAssets = SLMarketSecurityHelper.BuildAssetServices(priceLookupAccount);

                var allDates = _simDayResults.Values.SelectMany(days => days).Select(d => d.Date).ToList();
                if (allDates.Count > 0)
                {
                    DateTime minDate = allDates.Min();
                    DateTime maxDate = allDates.Max().AddDays(1);
                    foreach (var asset in priceAssets)
                        asset.Preload(minDate, maxDate);
                }
            }

            var managedTimeProvider = new ManagedDateTime();
            DateTimeService.GetInstance.SetDateProvider(managedTimeProvider);

            return _simDayResults
                .OrderBy(kv => kv.Key)
                .Select(kv =>
                {
                    var runningHighs = new Dictionary<string, decimal>();
                    foreach (var day in kv.Value)
                    {
                        managedTimeProvider.SetCurrentDate(day.Date);
                        day.Closes.Clear();
                        day.AllTimeHighs.Clear();
                        foreach (var asset in priceAssets)
                        {
                            decimal close = asset.GetLatestPrice();
                            decimal runningHigh = Math.Max(runningHighs.GetValueOrDefault(asset.TickerSymbol), close);
                            runningHighs[asset.TickerSymbol] = runningHigh;
                            day.Closes[asset.TickerSymbol] = close;
                            day.AllTimeHighs[asset.TickerSymbol] = runningHigh;
                        }
                    }
                    return new SimulationRunExport
                    {
                        Name = _simSettings.TryGetValue(kv.Key, out var s) ? s.Name : $"Run {kv.Key + 1}",
                        Days = kv.Value,
                        TrackedTickers = trackedTickers,
                    };
                })
                .ToList();
        }
        public enum DrawSpeed
        {
            Slow,    // 200 ms
            Medium,  // 100 ms
            Fast,    // 30 ms
            Instant  // no delay
        }

        [ObservableProperty]
        private int updateDelay = 100; // milliseconds between batches

        [ObservableProperty]
        private bool isPaused = true;

        // Cancel/Clear are only safe to offer once a running simulation has been paused —
        // this stops the "meant to hit Pause, hit Cancel" mistake from wiping a run in progress.
        public bool CanCancelSimulation => IsSimulationRunning && IsPaused;
        public bool CanClearSimulation => !IsSimulationRunning || IsPaused;

        partial void OnIsPausedChanged(bool value)
        {
            OnPropertyChanged(nameof(CanCancelSimulation));
            OnPropertyChanged(nameof(CanClearSimulation));
        }

        partial void OnIsSimulationRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanCancelSimulation));
            OnPropertyChanged(nameof(CanClearSimulation));
        }

        [ObservableProperty]
        private DrawSpeed selectedSpeed = DrawSpeed.Medium;

        // When off, the simulation still runs and results still get recorded for export,
        // but the chart is never updated — useful for fast, repeated debug runs.
        [ObservableProperty]
        private bool renderChartDuringSimulation = true;

        public ObservableCollection<DrawSpeed> SpeedOptions { get; } =
            new(Enum.GetValues<DrawSpeed>());

        [RelayCommand(CanExecute = nameof(CanTogglePlayPause))]
        
        private async Task TogglePlayPause()
        {
            if (IsSimulationRunning)
            {
                IsPaused = !IsPaused;
                return ;
            }

            IsSimulationRunning = true;
            IsPaused = false;
            ExportToExcelCommand.NotifyCanExecuteChanged();

            // Run the simulation in the background
            _ = Task.Run(RunInvestmentSimulation); // fire the long running task

        }


        // Target number of visual update steps for each speed — feels consistent regardless of date span
        private int GetTargetSteps() => SelectedSpeed switch
        {
            DrawSpeed.Slow    => 200,
            DrawSpeed.Medium  => 80,
            DrawSpeed.Fast    => 25,
            DrawSpeed.Instant => 1,
            _ => 80
        };

        private int GetBatchSize(int totalDays)
        {
            if (SelectedSpeed == DrawSpeed.Instant) return totalDays; // single flush
            int steps = GetTargetSteps();
            return Math.Max(1, totalDays / steps);
        }

        // Delay between batches (gives the animated "growth" feel)
        private TimeSpan GetDelay() => SelectedSpeed switch
        {
            DrawSpeed.Slow    => TimeSpan.FromMilliseconds(80),
            DrawSpeed.Medium  => TimeSpan.FromMilliseconds(40),
            DrawSpeed.Fast    => TimeSpan.FromMilliseconds(8),
            DrawSpeed.Instant => TimeSpan.Zero,
            _ => TimeSpan.FromMilliseconds(40)
        };

        [ObservableProperty]
        private int smoothCount = 120;


        [ObservableProperty] private bool isSimulationRunning = false;
        [RelayCommand]
        async Task RunInvestmentSimulation()
        {
            IsSimulationRunning = true;
            _quitSimulation = false;
            SetSimulationError(string.Empty);
            XAxes = new Axis[]
            {
                new DateTimeAxis(TimeSpan.FromDays(1), date =>
                    date.Month == 1 && date.Day <= 7
                        ? date.ToString("yyyy")
                        : date.ToString("MMM ''yy"))
                {
                    MinStep = TimeSpan.FromDays(28).Ticks,
                    LabelsRotation = -45,
                    TextSize = 11,
                }
            };
            simSettingsVM.SetSelections();
            ManagedDateTime managedTimeProvider = new ManagedDateTime();
            DateTimeService.GetInstance.SetDateProvider(managedTimeProvider);

            DateTime? endDate = simSettingsVM.ActiveSimSettings.EndDateOffset?.DateTime;
            DateTime? startDate = simSettingsVM.ActiveSimSettings.StartDateOffset?.DateTime;
            var series = ListOfLines[_numLinesUsed] as LineSeries<DateTimePoint>;
            if (series == null) throw new InvalidOperationException();

            //            List<DateTimePoint> points = new List<DateTimePoint>();
            // reset it:
            int numRendered = 0;

            // Ensure we have an ObservableCollection once, and clear it
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                if (series.Values is ObservableCollection<DateTimePoint> v)
                    v.Clear();
                else
                    series.Values = new ObservableCollection<DateTimePoint>();
            });

            // Pre-warm all asset caches for the full sim range — one DB query per asset,
            // and fail fast if any held ticker has no price data at all.
            try
            {
                await Task.Run(() => _accountService.PreloadForSimulation(
                    startDate.GetValueOrDefault(), endDate.GetValueOrDefault()));
            }
            catch (MissingMarketDataException ex)
            {
                SetSimulationError(ex.Message);
                IsSimulationRunning = false;
                IsPaused = true;
                return;
            }

            // Run the simulation loop in a background thread
            bool wasCancelled = false;
            await Task.Run(async () =>
            {
                int totalDays = (int)((endDate ?? DateTime.Now) - (startDate ?? DateTime.Now)).TotalDays;
                int batchSize = GetBatchSize(totalDays);
                TimeSpan delay = GetDelay();

                // Grows with every computed point — we replace series.Values each flush (one render per batch)
                var allPoints = new List<DateTimePoint>(totalDays);
                var dayResults = new List<SimulationDayResult>(totalDays);
                int dayIndex = 0;

                Debug.WriteLine($"startDate: {startDate} endDate: {endDate}  totalDays: {totalDays}  batchSize: {batchSize}");
                for (var date = startDate; date < endDate; date = date?.AddDays(1))
                {
                    while (IsPaused)
                    {
                        await Task.Delay(100);
                        if (_quitSimulation) break;
                    }

                    if (_quitSimulation)
                    {
                        Debug.WriteLine("Simulation cancelled.");
                        wasCancelled = true;
                        await _dispatcher.ExecuteOnMainThreadAsync(() => series.Values = new List<DateTimePoint>());
                        return;
                    }

                    managedTimeProvider.SetCurrentDate(date.GetValueOrDefault());
                    _accountService.RunDaysActivities();
                    // GetBalance() only sums invested assets — add cash on the sidelines
                    // (e.g. after a strategy sell-off) so this reflects the true account value.
                    decimal cashToday = _accountService.GetAccount().CashBalance;
                    decimal balanceToday = _accountService.GetBalance() + cashToday;

                    // Closes/AllTimeHighs are filled in later, at export time — see BuildExportRuns.
                    // That way "Track in Export" reflects whatever's checked *when you export*,
                    // not whatever was checked back when this run happened to be playing.
                    dayResults.Add(new SimulationDayResult
                    {
                        Date = date.GetValueOrDefault(),
                        Action = _accountService.LastAction,
                        Balance = balanceToday,
                        Cash = cashToday,
                        Contribution = _accountService.LastContribution,
                    });

                    var y = Math.Round((double)balanceToday, 2);
                    if (double.IsNaN(y) || double.IsInfinity(y)) { dayIndex++; continue; }

                    allPoints.Add(new DateTimePoint(date.GetValueOrDefault(), y));
                    numRendered++;
                    dayIndex++;

                    // Smooth start: render every day for the first 90 days so the line
                    // launches visibly; after that use the calculated batch size.
                    int effectiveBatch = dayIndex <= 90 ? 1 : batchSize;

                    if (RenderChartDuringSimulation && allPoints.Count % effectiveBatch == 0)
                    {
                        // Hand LiveCharts a private snapshot, not the live list.
                        // series.Values = ... only queues the update; LiveCharts' own
                        // throttled render pass reads it later, by which point this
                        // background loop may already be appending to allPoints again.
                        var toRender = new List<DateTimePoint>(allPoints);
                        await _dispatcher.ExecuteOnMainThreadAsync(() => series.Values = toRender);
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay);
                    }
                }

                // Final flush — always shows the finished line, even if intermediate painting was skipped.
                var finalPoints = new List<DateTimePoint>(allPoints);
                await _dispatcher.ExecuteOnMainThreadAsync(() => series.Values = finalPoints);
                _simDayResults[_numLinesUsed] = dayResults;
            });

            _quitSimulation = false;
            IsSimulationRunning = false;
            IsPaused = true;

            if (!wasCancelled)
            {
                _numLinesUsed++;
                Debug.WriteLine($"Done Testing dates - points rendered: {numRendered}");
                SimulationHasResults = true;
            }

            // NotifyCanExecuteChanged raises CanExecuteChanged synchronously on whatever thread calls it.
            // This whole method runs on a background thread (see TogglePlayPause's Task.Run), so without
            // marshalling back to the UI thread, Avalonia's Button never picks up the new CanExecute state.
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                TogglePlayPauseCommand.NotifyCanExecuteChanged();
                ExportToExcelCommand.NotifyCanExecuteChanged();
            });
        }

        
        private AccountService _accountService;
        readonly AppStateService _appState;
        public InvestmentPerformanceViewModel(AppShellService appShell, AppStateService appState):base(appShell)
        {
            _appState = appState;
            if (_appState.CurrentAccount != null) 
            {
                _myAccount = Account.DeepCopy(appState.CurrentAccount);
            }
            List<AssetService> assetServices = SLMarketSecurityHelper.BuildAssetServices(_myAccount);
            _dispatcher = appShell.Dispatcher;
//            CurrentHoldings = new ObservableCollection<SecurityHolding>(_myAccount.Assets);
            Debug.WriteLine($"Found client: {_myAccount.ClientId} and Owner: {_myAccount.OwnerId} with assetCount: {_myAccount.Assets.Count}");
            _accountService = new AccountService(_myAccount, assetServices);
            simSettingsVM = new SimulationSettingsViewModel(appShell, _accountService);
            SimSettingsVM.PropertyChanged += SimSettingsVM_PropertyChanged;
            Initialize();
        }
        public InvestmentPerformanceViewModel(AppShellService appShell, Account account) : base(appShell)
        {
            _myAccount = Account.DeepCopy(account);
            List<AssetService> assetServices = SLMarketSecurityHelper.BuildAssetServices(_myAccount);
            Debug.WriteLine($"Found client: {_myAccount.ClientId} and Owner: {_myAccount.OwnerId} with assetCount: {_myAccount.Assets.Count}");
            _accountService = new AccountService(_myAccount, assetServices);
            

            Initialize();
        }
 
        /* visibility/ clickable stuff. */
        [ObservableProperty]
        private bool isDayOfWeekPickerVisible;
        [ObservableProperty]
        private bool isDateEntryVisible;
        [ObservableProperty]
        private bool isSimulateButtonClickable;

        [ObservableProperty]
        private bool iSNOTDONE = false;

        private int _numLinesUsed = 0;
        private const int _MaxLines = 5;
        public StockSearchViewModel StockSearchVM { get; private set; }


        private async Task Initialize()
        {
            StockSearchVM = new StockSearchViewModel(_appShellService);
            StockSearchVM.OnHoldingAdded = holding =>
            {
                SimSettingsVM.ActiveSimSettings.Holdings.Add(holding);
                ShowSearchView = false;

            };
           await CreateLinesAndSettings();
        }

        private List<int> _selectedDates;
        public List<ISeries> series1 = new List<ISeries>();

        public ISeries[] Series { get; set; } =
        {
    new LineSeries<DateTimePoint>
    {
        Values = new DateTimePoint[]
        {
            new DateTimePoint(DateTime.Now.AddDays(-5), 10),
            new DateTimePoint(DateTime.Now.AddDays(-4), 15),
            new DateTimePoint(DateTime.Now.AddDays(-3), 8),
            new DateTimePoint(DateTime.Now.AddDays(-2), 18),
            new DateTimePoint(DateTime.Now.AddDays(-1), 12),
            new DateTimePoint(DateTime.Now, 20)
        },
        Name = "Portfolio A"
    },

    new LineSeries<DateTimePoint>
    {
        Values = new DateTimePoint[]
        {
            new DateTimePoint(DateTime.Now.AddDays(-5), 5),
            new DateTimePoint(DateTime.Now.AddDays(-4), 9),
            new DateTimePoint(DateTime.Now.AddDays(-3), 12),
            new DateTimePoint(DateTime.Now.AddDays(-2), 7),
            new DateTimePoint(DateTime.Now.AddDays(-1), 14),
            new DateTimePoint(DateTime.Now, 11)
        },
        Name = "Portfolio B"
    }
};



        [ObservableProperty]
        private ObservableCollection<ISeries> listOfLines = new();
        private async Task CreateLinesAndSettings()
        {
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                ListOfLines = new ObservableCollection<ISeries>();
            });
            _simSettings = new Dictionary<int, SimulationSettings>();
            for (int i = 0; i < _MaxLines; i++)
            {
                SimulationSettings simulationSettings = new SimulationSettings(_appShellService) { Name = $"Scenario #{i+1}" };
                foreach (var h in _accountService.Assets)
                simulationSettings.Holdings.Add(new SecurityHolding(h));
                _simSettings.Add(i,simulationSettings);
                SKColor color = SKColors.Black;
                switch (i)
                {
                    case 0: default: color = SKColors.Black; break;
                    case 1: color = SKColors.Green; break;
                    case 2: color = SKColors.Blue; break;
                    case 3: color = SKColors.Magenta; break;
                    case 4: color = SKColors.Red; break;
                }
                await _dispatcher.ExecuteOnMainThreadAsync(() =>
                {

                    ListOfLines.Add(new LineSeries<DateTimePoint>
                    {
                        Values = new List<DateTimePoint>(),//ObservableCollection<DateTimePoint>(),  using List, I must manually update)
                        Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },// new SolidColorPaint(SKColors.Red) { StrokeThickness = 4 },
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        AnimationsSpeed = TimeSpan.Zero,
                        EasingFunction = EasingFunctions.Lineal,
                        Name = simulationSettings.Name,
                    });
                });
            }
            simSettingsVM.ActiveSimSettings = _simSettings[0];
        }
        [RelayCommand]
        private void SelectSimulation(SimulationSettings sim)
        {
            if (sim == null) return;
            simSettingsVM.ActiveSimSettings = sim;
            // your existing OnActiveSimSettingsChanged will wire up the chart & form
        }
        [ObservableProperty]
        private Axis[] xAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => string.Empty)
            {
                MinStep = TimeSpan.FromDays(28).Ticks,
                LabelsPaint = null,
            }
        };

        partial void OnSimulationHasResultsChanged(bool value)
        {
            if (value)
            {
                XAxes = new Axis[]
                {
                    new DateTimeAxis(TimeSpan.FromDays(1), date =>
                        date.Month == 1 && date.Day <= 7
                            ? date.ToString("yyyy")
                            : date.ToString("MMM ''yy"))
                    {
                        MinStep = TimeSpan.FromDays(28).Ticks,
                        LabelsRotation = -45,
                        TextSize = 11,
                    }
                };
            }
            else
            {
                XAxes = new Axis[]
                {
                    new DateTimeAxis(TimeSpan.FromDays(1), date => string.Empty)
                    {
                        MinStep = TimeSpan.FromDays(28).Ticks,
                        LabelsPaint = null,
                    }
                };
            }
        }
    }
}
