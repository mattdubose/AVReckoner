using System;
using System.Linq;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;

namespace Reckoner.ViewModels
{
    public partial class DrawdownSettings : BaseViewModel
    {
        public DrawdownSettings(AppShellService appShell) : base(appShell) { }

        [ObservableProperty] ObservableCollection<SecurityHolding> holdings = new();
        [ObservableProperty] string name = string.Empty;
        [ObservableProperty] DateTimeOffset? startDateOffset = new DateTimeOffset(DateTime.Now);
        [ObservableProperty] DateTimeOffset? endDateOffset = new DateTimeOffset(DateTime.Now.AddYears(20));
        [ObservableProperty] decimal payoutAmount = 2000;
        [ObservableProperty] private ActionInterval payoutInterval = ActionInterval.Monthly;
        [ObservableProperty] private string payoutDayOfMonth = "1";
        [ObservableProperty] private DrawdownStrategy drawdownStrategy = DrawdownStrategy.FixedAmount;
        [ObservableProperty] decimal floorBalance = 0;

        public bool IsValid =>
            Holdings != null &&
            Holdings.Count > 0 &&
            PayoutAmount > 0 &&
            StartDateOffset < EndDateOffset;
    }

    public partial class DrawdownSimulationViewModel : BaseViewModel
    {
        readonly IUiThreadDispatcher _dispatcher;
        readonly AppStateService _appState;
        private Account _myAccount = new Account();
        private AccountService _accountService;

        [ObservableProperty] DrawdownSettings settings;
        [ObservableProperty] private bool simulationHasResults = false;
        [ObservableProperty] private bool isSimulationRunning = false;
        [ObservableProperty] private bool isPaused = true;
        [ObservableProperty] private decimal startingBalance;

        public List<DrawdownStrategy> DrawdownStrategies { get; } =
            Enum.GetValues<DrawdownStrategy>().ToList();

        public List<ActionInterval> PayoutIntervals { get; } =
            Enum.GetValues<ActionInterval>().ToList();

        public bool IsFloorVisible => Settings?.DrawdownStrategy == DrawdownStrategy.GuardedFloor;

        partial void OnSettingsChanged(DrawdownSettings? oldValue, DrawdownSettings newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= Settings_PropertyChanged;
            if (newValue != null) newValue.PropertyChanged += Settings_PropertyChanged;
            RunSimulationCommand.NotifyCanExecuteChanged();
        }

        private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SimulationHasResults = false;
            RunSimulationCommand.NotifyCanExecuteChanged();
            if (e.PropertyName == nameof(DrawdownSettings.DrawdownStrategy))
                OnPropertyChanged(nameof(IsFloorVisible));
        }

        partial void OnSimulationHasResultsChanged(bool value)
        {
            XAxes = value
                ? new Axis[]
                {
                    new DateTimeAxis(TimeSpan.FromDays(1), date =>
                        date.Month == 1 && date.Day <= 7
                            ? date.ToString("yyyy")
                            : date.ToString("MMM yyyy"))
                    {
                        MinStep = TimeSpan.FromDays(28).Ticks,
                        LabelsRotation = -45,
                        TextSize = 11,
                    }
                }
                : new Axis[]
                {
                    new DateTimeAxis(TimeSpan.FromDays(1), date => string.Empty)
                    {
                        MinStep = TimeSpan.FromDays(28).Ticks,
                        LabelsPaint = null,
                    }
                };
        }

        private bool CanRun() => Settings?.IsValid == true && !SimulationHasResults;

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunSimulation()
        {
            // TODO: implement drawdown simulation loop
            throw new NotImplementedException("Drawdown simulation not yet implemented.");
        }

        [RelayCommand]
        private void CancelSim() { IsPaused = true; IsSimulationRunning = false; }

        [RelayCommand]
        async Task ClearSims()
        {
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                foreach (var series in ListOfLines.OfType<LineSeries<DateTimePoint>>())
                    series.Values = new List<DateTimePoint>();
            });
            _numLinesUsed = 0;
            SimulationHasResults = false;
            RunSimulationCommand.NotifyCanExecuteChanged();
        }

        private const int _MaxLines = 5;
        private int _numLinesUsed = 0;

        [ObservableProperty]
        private ObservableCollection<ISeries> listOfLines = new();

        [ObservableProperty]
        private Axis[] xAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => string.Empty)
            {
                MinStep = TimeSpan.FromDays(28).Ticks,
                LabelsPaint = null,
            }
        };

        public DrawdownSimulationViewModel(AppShellService appShell, AppStateService appState) : base(appShell)
        {
            _appState = appState;
            _dispatcher = appShell.Dispatcher;

            if (_appState.CurrentAccount != null)
                _myAccount = Account.DeepCopy(appState.CurrentAccount);

            var assetServices = SLMarketSecurityHelper.BuildAssetServices(_myAccount);
            _accountService = new AccountService(_myAccount, assetServices);

            settings = new DrawdownSettings(appShell);
            foreach (var asset in _myAccount.Assets)
                settings.Holdings.Add(new SecurityHolding(asset));

            settings.PropertyChanged += Settings_PropertyChanged;

            StartingBalance = _accountService.GetBalance();

            _ = InitializeChartAsync();
        }

        private async Task InitializeChartAsync()
        {
            await _dispatcher.ExecuteOnMainThreadAsync(() =>
            {
                ListOfLines = new ObservableCollection<ISeries>();
                for (int i = 0; i < _MaxLines; i++)
                {
                    SKColor color = i switch
                    {
                        0 => SKColors.Black,
                        1 => SKColors.Green,
                        2 => SKColors.Blue,
                        3 => SKColors.Magenta,
                        _ => SKColors.Red,
                    };
                    ListOfLines.Add(new LineSeries<DateTimePoint>
                    {
                        Values = new List<DateTimePoint>(),
                        Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        AnimationsSpeed = TimeSpan.Zero,
                        EasingFunction = EasingFunctions.Lineal,
                        Name = $"Scenario #{i + 1}",
                    });
                }
            });
        }
    }
}
