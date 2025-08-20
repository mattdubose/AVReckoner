using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Reckoner.Models;
using Reckoner.Repositories;
using Reckoner.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
    public partial class SecuritiesSelectionViewModel : BaseViewModel
    {
        readonly AppStateService _appState;
        readonly IMarketSecurityRepository _marketSecurityRepository;

        // All available securities (for search)
        private readonly List<MarketSecurity> _allSecurities;

        // The list of current holdings (editable when IsEditMode = true)
        [ObservableProperty]
        ObservableCollection<SecurityHolding> heldSecurities;

        [ObservableProperty]
        ObservableCollection<MarketSecurity> filteredStocks;

        [ObservableProperty]
        string searchText;

        [ObservableProperty]
        MarketSecurity selectedStock;

        [ObservableProperty]
        private bool isAddButtonClickable;

        // Controls whether the UI is in “edit” vs. “view” mode
        [ObservableProperty]
        private bool isEditMode;

        // ────── Constructors ──────

        // DI constructor
        public SecuritiesSelectionViewModel(
            AppShellService shellService,
            AppStateService appState,
            IMarketSecurityRepository marketSecurityRepository):base(shellService)
        {
            _appState = appState;
            _marketSecurityRepository = marketSecurityRepository;

            _allSecurities = GetStockList();

            FilteredStocks = new ObservableCollection<MarketSecurity>();
            HeldSecurities = new ObservableCollection<SecurityHolding>();
            IsEditMode = false;
            IsAddButtonClickable = false;
        }

        // Parameterless fallback (e.g. for design-time)
        public SecuritiesSelectionViewModel(AppShellService appShell)
            : this(appShell, null!, null!) { }

        // ────── SearchText Changed Handler ──────

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
                return;
            }

            var filtered = _allSecurities
                .Where(stock =>
                    stock.TickerSymbol.Contains(value, System.StringComparison.OrdinalIgnoreCase)
                    || stock.Name.Contains(value, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredStocks.Clear();
            foreach (var stock in filtered)
                FilteredStocks.Add(stock);

            // If exactly one result, allow “Add”
            if (FilteredStocks.Count == 1)
            {
                SelectedStock = FilteredStocks[0];
                IsAddButtonClickable = true;
            }
            else
            {
                IsAddButtonClickable = false;
            }
        }

        // ────── Methods (will generate commands automatically) ──────

        [RelayCommand]
        private async Task AddSelectedItem()
        {
            if (!IsEditMode || SelectedStock == null)
                return;

            var newSecurity = new SecurityHolding(SelectedStock)
            {
                NumberOfShares = 0,
                ContributionPercentage = 0m
            };

            HeldSecurities.Add(newSecurity);

            IsAddButtonClickable = false;
            SearchText = string.Empty;
            FilteredStocks.Clear();
        }

        [RelayCommand]
        private void SelectStock()
        {
            if (SelectedStock == null)
                return;

            SearchText = $"{SelectedStock.Name} ({SelectedStock.TickerSymbol})";
            IsAddButtonClickable = true;
            FilteredStocks.Clear();
        }

        [RelayCommand]
        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
        }

        [RelayCommand]
        private async Task SaveChanges()
        {
            if (!IsEditMode)
                return;

            // TODO: Persist HeldSecurities to the DB, e.g. via AccountService:
            // int accountId = _appState.CurrentAccount.AccountID;
            // await _accountService.UpdateHoldingsAsync(accountId, HeldSecurities);

            IsEditMode = false;
        }

        [RelayCommand]
        private void RemoveSecurity(SecurityHolding holding)
        {
            if (!IsEditMode || holding == null)
                return;

            HeldSecurities.Remove(holding);
        }

        // ────── Helper to get all securities from repository ──────

        private List<MarketSecurity> GetStockList()
        {
            if (_marketSecurityRepository == null)
                return new List<MarketSecurity>();

            return _marketSecurityRepository.GetAll();
        }
    }
}
