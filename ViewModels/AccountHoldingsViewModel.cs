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
        readonly MarketDataSyncService _marketDataSync;

        private readonly List<MarketSecurity> _allSecurities;

        [ObservableProperty] ObservableCollection<SecurityHolding> holdings;
        [ObservableProperty] ObservableCollection<MarketSecurity> filteredStocks;
        [ObservableProperty] string searchText;
        [ObservableProperty] MarketSecurity selectedStock;
        [ObservableProperty] bool isAddButtonClickable;
        [ObservableProperty] decimal cashBalance;
        [ObservableProperty] decimal assetBalance;
        [ObservableProperty] string accountNumber;
        [ObservableProperty] bool isEditMode;

        // Online search / download
        [ObservableProperty] ObservableCollection<ExternalSecurityResult> externalResults = new();
        [ObservableProperty] ExternalSecurityResult? selectedExternalResult;
        [ObservableProperty] bool canSearchOnline;
        [ObservableProperty] bool hasExternalResults;
        [ObservableProperty] bool canDownload;
        [ObservableProperty] bool isDownloading;
        [ObservableProperty] string downloadStatus = string.Empty;
        [ObservableProperty] bool hasStatusMessage;

        IServiceProvider _services;
        Account _myAccount;
        AccountService _accountService;

        public AccountHoldingsViewModel(
            AppShellService appShell,
            AppStateService appState,
            IMarketSecurityRepository marketSecurityRepository,
            IAccountRepository accountRepository,
            MarketDataSyncService marketDataSync,
            IServiceProvider services) : base(appShell)
        {
            _services = services;
            _appState = appState;
            _marketSecurityRepository = marketSecurityRepository;
            _accountRepository = accountRepository;
            _marketDataSync = marketDataSync;
            holdings = new ObservableCollection<SecurityHolding>();
            filteredStocks = new ObservableCollection<MarketSecurity>();
            _allSecurities = _marketSecurityRepository.GetAll();
            _myAccount = _appState.CurrentAccount;
            List<AssetService> assetServices = SLMarketSecurityHelper.BuildAssetServices(_myAccount);

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
            _appState.PropertyChanged += async (_, e) =>
            {
                if (e.PropertyName == nameof(AppStateService.CurrentAccount))
                    await LoadHoldingsAsync();
            };
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            if (_appState.CurrentAccount != null)
                await LoadHoldingsAsync();
        }

        public IAsyncRelayCommand LoadHoldingsCommand { get; }
        public IRelayCommand ToggleEditModeCommand { get; }
        public IAsyncRelayCommand SaveChangesCommand { get; }
        public IAsyncRelayCommand AddSelectedItemCommand { get; }

        public async Task LoadHoldingsAsync()
        {
            Holdings.Clear();
            if (_myAccount == null) return;
            foreach (var h in _myAccount.Assets)
            {
                if (string.IsNullOrEmpty(h.Name))
                {
                    var match = _allSecurities.FirstOrDefault(s =>
                        s.TickerSymbol.Equals(h.TickerSymbol, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        h.Name = match.Name;
                }
                Holdings.Add(h);
            }
        }

        [RelayCommand]
        async Task OpenDrawdownSimulation()
        {
            if (_appState.CurrentAccount == null) return;
            await _appShellService.NavigateToAsync<DrawdownSimulationViewModel>();
        }

        [RelayCommand]
        async Task OpenSimulationCommand()
        {
            if (_appState.CurrentAccount == null) return;
            await _appShellService.NavigateToAsync<InvestmentPerformanceViewModel>();
        }

        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            if (IsEditMode)
            {
                SearchText = string.Empty;
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
            }
        }

        private async Task SaveChangesAsync()
        {
            if (!IsEditMode || _myAccount == null) return;
            var toRemove = Holdings
                .Where(h => h.NumberOfShares == 0 && (h.ContributionPercentage == null || h.ContributionPercentage == 0))
                .ToList();
            foreach (var r in toRemove)
                Holdings.Remove(r);
            _myAccount.Assets = Holdings.ToList();
            await _accountRepository.UpdateAccountAsync(_myAccount);
            IsEditMode = false;
            await LoadHoldingsAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            if (!IsEditMode)
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
                return;
            }

            // Reset online search state whenever the user types
            ExternalResults.Clear();
            HasExternalResults = false;
            SelectedExternalResult = null;
            CanDownload = false;
            SetStatus(string.Empty);

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
                CanSearchOnline = false;
                return;
            }

            var matches = _allSecurities
                .Where(s =>
                    s.TickerSymbol.Contains(value, StringComparison.OrdinalIgnoreCase)
                    || s.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredStocks.Clear();
            foreach (var m in matches)
                FilteredStocks.Add(m);

            IsAddButtonClickable = FilteredStocks.Count == 1;
            if (IsAddButtonClickable)
                SelectedStock = FilteredStocks[0];

            CanSearchOnline = FilteredStocks.Count == 0 && !IsDownloading;
        }

        partial void OnSelectedStockChanged(MarketSecurity value)
        {
            if (value == null) return;
            SearchText = $"{value.Name} ({value.TickerSymbol})";
            FilteredStocks.Clear();
            IsAddButtonClickable = true;
        }

        partial void OnSelectedExternalResultChanged(ExternalSecurityResult? value)
        {
            CanDownload = value != null && !IsDownloading;
        }

        private async Task AddSelectedItemAsync()
        {
            if (!IsEditMode || SelectedStock == null) return;
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

        [RelayCommand]
        async Task SearchOnline()
        {
            if (!_marketDataSync.IsConfigured)
            {
                SetStatus("fipy.exe not found. Build the fiPy project first.");
                return;
            }
            IsDownloading = true;
            CanSearchOnline = false;
            SetStatus("Searching online...");
            ExternalResults.Clear();
            try
            {
                var results = await _marketDataSync.SearchExternalAsync(SearchText);
                foreach (var r in results)
                    ExternalResults.Add(r);
                HasExternalResults = ExternalResults.Count > 0;
                SetStatus(ExternalResults.Count == 0 ? "No results found." : string.Empty);
            }
            catch (Exception ex)
            {
                SetStatus($"Search failed: {ex.Message}");
            }
            finally
            {
                IsDownloading = false;
            }
        }

        [RelayCommand]
        async Task DownloadTicker()
        {
            if (SelectedExternalResult is null) return;
            if (!_marketDataSync.IsConfigured)
            {
                SetStatus("fipy.exe not found. Build the fiPy project first.");
                return;
            }

            var ticker = SelectedExternalResult.Ticker;
            IsDownloading = true;
            CanDownload = false;
            SetStatus($"Downloading {ticker}...");

            var progress = new Progress<string>(line =>
            {
                if (line.StartsWith("PROGRESS:") || line.StartsWith("SUCCESS:") ||
                    line.StartsWith("ERROR:") || line.StartsWith("WARNING:"))
                    SetStatus(line[(line.IndexOf(':') + 1)..].Trim());
            });

            try
            {
                var result = await _marketDataSync.AddTickerAsync(ticker, progress);
                if (result.Success)
                {
                    var refreshed = _marketSecurityRepository.GetAll();
                    _allSecurities.Clear();
                    _allSecurities.AddRange(refreshed);

                    ExternalResults.Clear();
                    HasExternalResults = false;
                    SelectedExternalResult = null;
                    SetStatus(string.Empty);

                    // Trigger local search so the new ticker appears immediately
                    SearchText = ticker;
                }
                else
                {
                    var msg = result.ErrorOutput.Trim();
                    SetStatus(string.IsNullOrEmpty(msg) ? "Download failed." : $"Error: {msg}");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Download failed: {ex.Message}");
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private void SetStatus(string message)
        {
            DownloadStatus = message;
            HasStatusMessage = !string.IsNullOrEmpty(message);
        }
    }
}
