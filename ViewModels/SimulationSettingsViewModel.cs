using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Utilities;
using Reckoner.Models;
using Reckoner.Repositories;

namespace Reckoner.ViewModels
{
    public enum ContributionBreakdown
    {
        PerContribution,
        PerYear,
        PerMonth,
    }
    public partial class SimulationSettings : BaseViewModel
    {
        public SimulationSettings(AppShellService appShell) :base(appShell){ }
        [ObservableProperty] ObservableCollection<SecurityHolding> holdings = new();
        [ObservableProperty] string name = string.Empty;
        [ObservableProperty] private ContributionBreakdown contributionHelper = ContributionBreakdown.PerContribution;
        [ObservableProperty] DateTime startDate = new(2002, 1, 1);
        [ObservableProperty] DateTime endDate = DateTime.Now;
        [ObservableProperty] decimal contributionAmount = 100;
        [ObservableProperty] private DayOfWeek selectedDayOfWeek = DayOfWeek.Friday;
        [ObservableProperty] private string contributionDates = "1";
        [ObservableProperty] private string dateHelperText;
        [ObservableProperty] private ActionInterval contributionInterval;
        [ObservableProperty] private ActionInterval rebalanceInterval = ActionInterval.Yearly;
        [ObservableProperty] private bool dividendReinvestment = true;
        [ObservableProperty] decimal initialCash = 0;
        [ObservableProperty] private InvestmentStrategy strategy = InvestmentStrategy.None;
    };


    public partial class SimulationSettingsViewModel : BaseViewModel
    {

        public SimulationSettingsViewModel(AppShellService appShell, AccountService accountService) : base(appShell)
        {
            _accountService = accountService;
            var newSettings = new SimulationSettings(_appShellService);
            foreach (var asset in _accountService.GetAccount().Assets)
            {
                newSettings.Holdings.Add(asset);
            }

            // This assignment triggers [ObservableProperty] notification:
            ActiveSimSettings = newSettings;
            ActiveSimSettings.PropertyChanged += (_, __) => UpdateComputed();
            BuildPickers();
            InitilizeSelections();
        }
        partial void OnActiveSimSettingsChanged(SimulationSettings oldValue, SimulationSettings newValue)
        {
            if (oldValue != null)
                oldValue.PropertyChanged -= ActiveSimSettings_PropertyChanged;

            if (newValue != null)
                newValue.PropertyChanged += ActiveSimSettings_PropertyChanged;

            UpdateComputed(); // Run once on reassignment
        }

        partial void OnSelectedFwTickerChanged(SecurityHolding value)
        {
            UpdateComputed();
        }
        private void ActiveSimSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateComputed(); // Called when any inner property changes
        }

        public bool IsValid
        {
            get
            {
                if (ActiveSimSettings == null || ActiveSimSettings.Holdings == null)
                    return false;

                if (ActiveSimSettings.ContributionAmount <= 0)
                    return false;

                if (ActiveSimSettings.StartDate >= ActiveSimSettings.EndDate) return false;

                decimal total = ActiveSimSettings.Holdings.Sum(h => h.ContributionPercentageX100);
                if (total != 100) return false;

                if (ActiveSimSettings.Strategy == InvestmentStrategy.FWStrategy && SelectedFwTicker == null)
                {
                    return false; // FW Strategy requires a selected ticker
                }

                return true;
            }
        }

        public bool ShowDayPicker =>
            ActiveSimSettings.ContributionInterval == ActionInterval.Weekly ||
            ActiveSimSettings.ContributionInterval == ActionInterval.BiWeekly;

        public bool ShowDateEntry =>
            ActiveSimSettings.ContributionInterval == ActionInterval.Monthly ||
            ActiveSimSettings.ContributionInterval == ActionInterval.SemiMonthly;
        public bool UseFWStrategy =>
           ActiveSimSettings.Strategy == InvestmentStrategy.FWStrategy;

        public string DateHelperText =>
            ActiveSimSettings.ContributionInterval switch
            {
                ActionInterval.Monthly => "Enter Day of Month contributions are to be made.",
                ActionInterval.SemiMonthly => "Enter Days of Month contributions are to be made, separate with comma.",
                _ => string.Empty
            };  /* picker helpers. */

        

        // 2) Which ticker the user picked for FW Strategy:
        [ObservableProperty]
        SecurityHolding selectedFwTicker = null;


        // When user checks the box, default SelectedFwTicker to first holding (if any):
        //partial void OnUseFwStrategyChanged(bool oldValue, bool newValue)
        //{
        //    if (newValue && ActiveSimSettings.Holdings.Any())
        //        SelectedFwTicker = ActiveSimSettings.Holdings[0];
        //}
        //
        public void UpdateComputed()
        {
            OnPropertyChanged(nameof(IsValid));
            OnPropertyChanged(nameof(ShowDayPicker));
            OnPropertyChanged(nameof(ShowDateEntry));
            OnPropertyChanged(nameof(DateHelperText));
            OnPropertyChanged(nameof(UseFWStrategy));
            OnPropertyChanged(nameof(SelectedFwTicker));
        }

        [ObservableProperty]
        FWStrategySettings strategySettings = new();
        public void SetSelections()
        {
            Account account = _accountService.GetAccount();
            if (account.InvestmentSchedule == null)
            {
                account.InvestmentSchedule = new InvestmentSchedule();
            }
            if (account.InvestmentSchedule.ContributionFrequency == null)
            {
                account.InvestmentSchedule.ContributionFrequency = new ActionFrequency();
            }
            if (account.InvestmentSchedule.AdjustmentFrequency == null)
            {
                account.InvestmentSchedule.AdjustmentFrequency = new ActionFrequency();
            }
            account.InvestmentSchedule.BeginContributions = ActiveSimSettings.StartDate;
            account.InvestmentSchedule.EndContributions = ActiveSimSettings.EndDate;
            account.InvestmentAmount = ActiveSimSettings.ContributionAmount;
            account.InvestmentSchedule.AdjustmentFrequency.Interval = ActiveSimSettings.RebalanceInterval;
            account.InvestmentSchedule.ContributionFrequency.DayOfWeek = ActiveSimSettings.SelectedDayOfWeek;
            account.InvestmentSchedule.ContributionFrequency.Interval = ActiveSimSettings.ContributionInterval;
            account.InvestmentSchedule.ContributionFrequency.Dates = CsvReader.GetListOfInts(ActiveSimSettings.ContributionDates);
            account.InvestmentSchedule.AdjustDates();
            account.Strategy = ActiveSimSettings.Strategy;
            account.StackedActivities.Add(new ActivityHolder(Models.Action.Contribution, ActiveSimSettings.InitialCash, ActiveSimSettings.StartDate));
            account.StrategyConfiguration = StrategySettings; // FW Strategy settings
            account.DividentReinvestment = ActiveSimSettings.DividendReinvestment;

            StrategySettings.TickerToEvaluate = SelectedFwTicker?.TickerSymbol;
            account.Assets.Clear();
            foreach (var h in ActiveSimSettings.Holdings)
            {
                h.NumberOfShares = 0; /* WMD - !!! clearing for purpose of investment scenario */
                account.Assets.Add(h);
            }
            _accountService.Assets = SLMarketSecurityHelper.BuildAssetServices(account);
            if (ActiveSimSettings.Strategy == InvestmentStrategy.FWStrategy)
            {
                foreach (var asset in _accountService.Assets)
                {
                    if (asset.TickerSymbol == SelectedFwTicker.TickerSymbol)
                    {
                        _accountService.InvestmentStrategyService = new FWInvestmentStrategy(new List<AssetService> { asset }, strategySettings);
                    }
                }
            }

        }
        private void InitilizeSelections()
        {
            Account account = _accountService.GetAccount();
            if (account.InvestmentSchedule != null)
            {
                InvestmentSchedule investmentSchedule = account.InvestmentSchedule;

                if (investmentSchedule.ContributionFrequency != null)
                {
                    ActiveSimSettings.StartDate = investmentSchedule?.BeginContributions ?? ActiveSimSettings.StartDate;
                    ActiveSimSettings.EndDate = investmentSchedule?.EndContributions ?? ActiveSimSettings.EndDate;
                    ActiveSimSettings.ContributionAmount = account.InvestmentAmount;
                    if (account.InvestmentSchedule.ContributionFrequency != null)
                    {
                        ActiveSimSettings.ContributionInterval = account.InvestmentSchedule.ContributionFrequency.Interval;
                        ActiveSimSettings.SelectedDayOfWeek = account.InvestmentSchedule.ContributionFrequency?.DayOfWeek ?? ActiveSimSettings.SelectedDayOfWeek;
                        _selectedDates = account.InvestmentSchedule.ContributionFrequency?.Dates ?? _selectedDates;
                    }
                    if (account.InvestmentSchedule.AdjustmentFrequency != null)
                    {
                        ActiveSimSettings.RebalanceInterval = account.InvestmentSchedule.AdjustmentFrequency.Interval;
                    }
                }
            }
        }

        public List<DayOfWeek> DaysOfWeek { get; } = new List<DayOfWeek>();
        public List<ActionInterval> ContributionFrequencies { get; } = new List<ActionInterval>();
        public List<ContributionBreakdown> BreadownOptions { get; } = new List<ContributionBreakdown>();
        public List<InvestmentStrategy> InvestmentStrategies { get; } = new List<InvestmentStrategy>();
        private void BuildPickers()
        {
            foreach (ContributionBreakdown value in Enum.GetValues(typeof(ContributionBreakdown)))
            {
                BreadownOptions.Add(value);
            }
            foreach (DayOfWeek value in Enum.GetValues(typeof(System.DayOfWeek)))
            {
                DaysOfWeek.Add(value);
            }
            foreach (ActionInterval value in Enum.GetValues(typeof(ActionInterval)))
            {
                ContributionFrequencies.Add(value);
            }
            foreach (InvestmentStrategy value in Enum.GetValues(typeof(InvestmentStrategy)))
            {
                InvestmentStrategies.Add(value);
            }

        }

        private List<int> _selectedDates;

        private AccountService _accountService;

        [ObservableProperty]
        private SimulationSettings activeSimSettings;// = new SimulationSettings();


        [RelayCommand(CanExecute = nameof(CanOpenSettings))]
        private async Task OpenStrategySettingsAsync()
        {
            /*
            var popup = new FWStrategySettingsPopup(new FWStrategySettingsViewModel(StrategySettings));
            var result = await Shell.Current.CurrentPage.ShowPopupAsync(popup);
            if (result is FWStrategySettings updatedSettings)
            {
                StrategySettings = updatedSettings;
            }*/
        }
        private bool CanOpenSettings() => UseFWStrategy;
    }
}
