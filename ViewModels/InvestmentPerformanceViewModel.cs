using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using Reckoner.Repositories;
using Reckoner.Utilities;
using PurpleValley.Utilities;

namespace Reckoner.ViewModels
{

    public partial class InvestmentPerformanceViewModel : BaseViewModel
    {
        IUiThreadDispatcher _dispatcher;
    
        [ObservableProperty]
        SimulationSettingsViewModel simSettingsVM;
        private Account _myAccount = new Account();
        Dictionary<int, SimulationSettings> _simSettings;
        bool _quitSimulation = false;
        [ObservableProperty]
        bool showSearchView = false;

        [RelayCommand]
        private void AddNewHolding()
        {
            ShowSearchView = true;
        }
        [RelayCommand] void CancelSim()  { _quitSimulation = true; }

        [RelayCommand] void ClearSims() 
        {
            _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                foreach (var series in ListOfLines.OfType<LineSeries<DateTimePoint>>())
                {
                    series.Values = new List<DateTimePoint>();
                }
            });
            _numLinesUsed = 0;
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

        [ObservableProperty]
        private DrawSpeed selectedSpeed = DrawSpeed.Medium;

        public ObservableCollection<DrawSpeed> SpeedOptions { get; } =
            new(Enum.GetValues<DrawSpeed>());


        [RelayCommand]
        private Task TogglePlayPause()
        {
            if (IsSimulationRunning)
            {
                IsPaused = !IsPaused;
                return Task.CompletedTask;
            }

            IsSimulationRunning = true;
            IsPaused = false;

            // Run the simulation in the background
            Task.Run(async () =>
            {
                try
                {
                    await RunInvestmentSimulation();
                }
                catch 
                {
                    Console.WriteLine("can't start the investement simulaton!!!");
                }
                finally
                {
                    IsSimulationRunning = false;
                    IsPaused = true;
                }
            });

            return Task.CompletedTask;
        }


        private TimeSpan GetDelay()
        {
            return SelectedSpeed switch
            {
                DrawSpeed.Slow => TimeSpan.FromMilliseconds(100),
                DrawSpeed.Medium => TimeSpan.FromMilliseconds(50),
                DrawSpeed.Fast => TimeSpan.FromMilliseconds(1),
                DrawSpeed.Instant => TimeSpan.Zero,
                _ => TimeSpan.FromMilliseconds(100)
            };
        }

        [ObservableProperty]
        private int smoothCount = 120;


        [ObservableProperty] private bool isSimulationRunning = false;

        [RelayCommand]
        async Task RunInvestmentSimulation()
        {
            IsSimulationRunning = true;
            _quitSimulation = false;
            simSettingsVM.SetSelections();
            ManagedDateTime managedTimeProvider = new ManagedDateTime();
            DateTimeService.GetInstance.SetDateProvider(managedTimeProvider);

            DateTime endDate = simSettingsVM.ActiveSimSettings.EndDate;
            DateTime startDate = simSettingsVM.ActiveSimSettings.StartDate;
            var series = ListOfLines[_numLinesUsed] as LineSeries<DateTimePoint>;
            if (series == null) throw new InvalidOperationException();

            //            List<DateTimePoint> points = new List<DateTimePoint>();
            // reset it:
            await _dispatcher.ExecuteOnMainThreadAsync(() => series.Values = new List<DateTimePoint>());
            // Run the simulation loop in a separate thread
            await Task.Run(async () =>
            {
                var workingList = new List<DateTimePoint>();
                var buffer = new List<DateTimePoint>();

                for (var date = startDate; date < endDate; date = date.AddDays(1))
                {
                    while (IsPaused)
                    {
                        await Task.Delay(100); // Poll every 100ms while paused
                        if (_quitSimulation) break;
                    }
                    if (_quitSimulation)
                    {
                        Debug.WriteLine("Simulation cancelled by user.");
                        buffer.Clear();
                        IsSimulationRunning = false;
                        await _dispatcher.ExecuteOnMainThreadAsync(() =>
                        {
                            series.Values = new List<DateTimePoint>();
                        });

                        return;
                    }
                    managedTimeProvider.SetCurrentDate(date);
                    _accountService.RunDaysActivities();
                    var y = Math.Round((double)_accountService.GetBalance(), 2);
                    if (double.IsNaN(y) || double.IsInfinity(y)) continue;

                    buffer.Add(new DateTimePoint(date, y));
                    // Only flush if enough time has passed or in Instant mode
                    if (SelectedSpeed != DrawSpeed.Instant && buffer.Count >= 200)
                    {
                        var flush = buffer.ToList();
                        buffer.Clear();
                        await _dispatcher.ExecuteOnMainThreadAsync(() =>
                        {
                            workingList.AddRange(flush);
                            series.Values = new List<DateTimePoint>(workingList); // Forces redraw
                        });

                        var delay = GetDelay();
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay);
                    }
              }

                // Final flush
                if (buffer.Count > 0)
                {
                    var flush = buffer.ToList();
                    await _dispatcher.ExecuteOnMainThreadAsync(() =>
                    {
                        workingList.AddRange(flush);
                        series.Values = new List<DateTimePoint>(workingList);
                    });
                }
            });

            _numLinesUsed++;
            Debug.WriteLine("Done Testing dates");
                IsSimulationRunning = false;
          // Re-enable the simulate button after completion
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

//            CurrentHoldings = new ObservableCollection<SecurityHolding>(_myAccount.Assets);
            Debug.WriteLine($"Found client: {_myAccount.ClientId} and Owner: {_myAccount.OwnerId} with assetCount: {_myAccount.Assets.Count}");
            _accountService = new AccountService(_myAccount, assetServices);
            simSettingsVM = new SimulationSettingsViewModel(appShell, _accountService);
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


        private void Initialize()
        {
            StockSearchVM = new StockSearchViewModel(_appShellService);
            StockSearchVM.OnHoldingAdded = holding =>
            {
                SimSettingsVM.ActiveSimSettings.Holdings.Add(holding);
                ShowSearchView = false;

            };
            CreateLinesAndSettings();
        }

        private ObservableCollection<DateTimePoint> myChangedData;
        private List<int> _selectedDates;

        public List<ISeries> ListOfLines { get; set; }
        private void CreateLinesAndSettings()
        {
            _dispatcher.ExecuteOnMainThreadAsync(() =>
            {

                ListOfLines = new List<ISeries>();
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
                _dispatcher.ExecuteOnMainThreadAsync(() =>
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
        public Axis[] XAxes { get; set; } =
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString("MMMM yy"))
        };
    }
}
