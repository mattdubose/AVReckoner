using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reckoner.Models;
using Reckoner.Repositories;
using Reckoner.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
    public partial class AccountHoldingsViewModel : BaseViewModel
    {
        readonly AppStateService _appState;
        readonly IMarketSecurityRepository _marketSecurityRepository;

        readonly IAccountRepository _accountRepository;
        // 1) The “search” pool of all securities:
        private readonly List<MarketSecurity> _allSecurities;

        // 2) Current holdings for this account:
        [ObservableProperty]
        ObservableCollection<SecurityHolding> holdings;

        // 3) The filtered search results (when the user types in the search bar):
        [ObservableProperty]
        ObservableCollection<MarketSecurity> filteredStocks;

        // 4) The text the user typed in the search bar:
        [ObservableProperty]
        string searchText;

        // 5) The stock the user tapped in the filtered list:
        [ObservableProperty]
        MarketSecurity selectedStock;

        // 6) Whether the “Add” button is enabled (only when a valid stock is chosen):
        [ObservableProperty]
        bool isAddButtonClickable;

        [ObservableProperty]
        decimal cashBalance;
        [ObservableProperty]
        decimal assetBalance;
        [ObservableProperty]
        string accountNumber;
        // 7) Are we in “edit mode” or “view mode”?
        [ObservableProperty]
        bool isEditMode;
        IServiceProvider _services;
        Account _myAccount;
        AccountService _accountService;
        public AccountHoldingsViewModel(
            AppShellService appShell,
            AppStateService appState,
            IMarketSecurityRepository marketSecurityRepository,
            IAccountRepository accountRepository, IServiceProvider services)   : base(appShell)// ← no extra registration needed
        {   _services = services;
            _appState = appState;
            _marketSecurityRepository = marketSecurityRepository;
            _accountRepository = accountRepository;
            holdings = new ObservableCollection<SecurityHolding>();
            filteredStocks = new ObservableCollection<MarketSecurity>();
            _allSecurities = _marketSecurityRepository.GetAll();
            _myAccount = _appState.CurrentAccount;
            List<AssetService> assetServices = SLMarketSecurityHelper.BuildAssetServices(_myAccount);

            //            List<AssetService> assetServices = PGMarketSecurityHelper.BuildAssetServices(_myAccount);
            Debug.WriteLine($"Found client: {_myAccount.ClientId} and Owner: {_myAccount.OwnerId} with assetCount: {_myAccount.Assets.Count}");
            _accountService = new AccountService(_myAccount, assetServices);
            CashBalance = _myAccount.CashBalance;
            AssetBalance = _accountService.GetBalance();
            AccountNumber = _myAccount.AccountId.ToString();
            IsEditMode = false;
            IsAddButtonClickable = false;

            LoadHoldingsCommand = new AsyncRelayCommand(LoadHoldingsAsync);
            ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
            SaveChangesCommand = new AsyncRelayCommand(SaveChangesAsync);
            AddSelectedItemCommand = new AsyncRelayCommand(AddSelectedItemAsync);

            // Whenever the account changes, reload holdings:
            _appState.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(AppStateService.CurrentAccount))
                    await LoadHoldingsAsync();
            };
        }

        // Command properties (automatically wired by the toolkit):
        public IAsyncRelayCommand LoadHoldingsCommand { get; }
        public IRelayCommand ToggleEditModeCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand AddSelectedItemCommand { get; }

        // Reload holdings from the current account’s Assets:
        public async Task LoadHoldingsAsync()
        {
            
            Holdings.Clear();
            
            if (_myAccount == null) return;

            foreach (var h in _myAccount.Assets)
                Holdings.Add(h);
        }
        [RelayCommand]
        async Task OpenSimulationCommand()
        {
            // 1) stash whatever data Performance needs in AppState
            var account = _appState.CurrentAccount;
            if (account == null)
                return;

            // if you need holdings separately:
//            _appState.CurrentHoldings = Holdings.ToList();
/*
            // 2) resolve the Performance page (and its VM) from DI
            var perfPage = _services.GetRequiredService<InvestmentPerformancePage>();
*/
            // 3) navigate
            await _appShellService.NavigateToAsync<InvestmentPerformanceViewModel>();
        }
        // Toggle between view and edit mode:
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;

            // When entering edit mode, clear any old search state:
            if (IsEditMode)
            {
                SearchText = string.Empty;
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
            }
        }

        // “Save” persists changes by writing back to account.Assets, then exit edit mode:
        private async Task SaveChangesAsync()
        {
            if (!IsEditMode) return;
            
            if (_myAccount == null)
                return;

            // Prune any holdings with zero shares AND zero contribution:
            var toRemove = Holdings
                .Where(h => h.NumberOfShares == 0 && (h.ContributionPercentage == null || h.ContributionPercentage == 0))
                .ToList();
            foreach (var r in toRemove)
                Holdings.Remove(r);

            // Assign back to the account model:
            _myAccount.Assets = Holdings.ToList();

            // TODO: persist 'account' to the database here if needed.
            await _accountRepository.UpdateAccountAsync(_myAccount);
            IsEditMode = false;
            await LoadHoldingsAsync();
        }

        // Whenever SearchText changes, update FilteredStocks:
        partial void OnSearchTextChanged(string value)
        {
            if (!IsEditMode)
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
            }
            else
            {
                var matches = _allSecurities
                    .Where(s =>
                        s.TickerSymbol.Contains(value, StringComparison.OrdinalIgnoreCase)
                        || s.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                FilteredStocks.Clear();
                foreach (var m in matches)
                    FilteredStocks.Add(m);

                // Only enable “Add” when there's exactly one match:
                IsAddButtonClickable = (FilteredStocks.Count == 1);
                if (IsAddButtonClickable)
                    SelectedStock = FilteredStocks[0];
            }
        }

        // **Only one** OnSelectedStockChanged method. Do NOT duplicate this.
        // Called when the user taps a stock in the filtered list (via SelectedItem binding):
        partial void OnSelectedStockChanged(MarketSecurity value)
        {
            if (value == null) return;

            // Populate the SearchBar with the chosen ticker+name:
            SearchText = $"{value.Name} ({value.TickerSymbol})";

            // Clear out the filtered list so that AddSelectedItemAsync will work:
            FilteredStocks.Clear();

            // Enable the Add button now that a stock is selected:
            IsAddButtonClickable = true;
        }

        // Add the chosen stock as a new holding (0 shares, 0% contribution):
        private async Task AddSelectedItemAsync()
        {
            if (!IsEditMode) return;

            if (SelectedStock == null) return;

            var newHolding = new SecurityHolding(SelectedStock)
            {
                NumberOfShares = 0,
                ContributionPercentage = 0m
            };
            Holdings.Add(newHolding);

            SearchText = string.Empty;
            FilteredStocks.Clear();
            IsAddButtonClickable = false;
            SelectedStock = null;
        }
    }
}
