using System;
using System.Collections.Generic;
using Reckoner.Repositories;
using Dapper;
using System.Linq;

namespace Reckoner.ViewModels
{
    public partial class StockSearchViewModel : BaseViewModel
    {
        private List<MarketSecurity> _allSecurities = new();
    
        [ObservableProperty] string searchText = string.Empty;
        [ObservableProperty] ObservableCollection<MarketSecurity> filteredStocks = new();
        [ObservableProperty] MarketSecurity? selectedStock;
        [ObservableProperty] bool isAddButtonClickable;
        private IMarketSecurityRepository _marketSecurityRepository;
        public Action<SecurityHolding>? OnHoldingAdded { get; set; }

        public StockSearchViewModel(AppShellService appShell) : this(appShell, new SqliteMarketSecurityRepository("ReckonerDB.db"))
        {
        }
        public StockSearchViewModel(AppShellService appShell, IMarketSecurityRepository marketSecurityRepository): base(appShell)
        {
                _marketSecurityRepository = marketSecurityRepository;
                // Initialize with an empty list
                _allSecurities = _marketSecurityRepository.GetAll();
            FilteredStocks = new ObservableCollection<MarketSecurity>();
            IsAddButtonClickable = false;
        }
        public void SetAvailableSecurities(List<MarketSecurity> allSecurities)
        {
            _allSecurities = allSecurities;
        }
    
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredStocks.Clear();
                IsAddButtonClickable = false;
                return;
            }
    
            var matches = _allSecurities
                .Where(s => s.TickerSymbol.Contains(value, StringComparison.OrdinalIgnoreCase)
                         || s.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();
    
            FilteredStocks.Clear();
            foreach (var match in matches)
                FilteredStocks.Add(match);
    
            IsAddButtonClickable = FilteredStocks.Count == 1;
            if (IsAddButtonClickable)
                SelectedStock = FilteredStocks[0];
        }
    
        partial void OnSelectedStockChanged(MarketSecurity? value)
        {
            if (value == null) return;
    
            SearchText = $"{value.Name} ({value.TickerSymbol})";
            FilteredStocks.Clear();
            IsAddButtonClickable = true;
        }
    
        [RelayCommand]
        private void AddSelectedItem()
        {
            if (SelectedStock == null) return;
    
            var newHolding = new SecurityHolding(SelectedStock)
            {
                NumberOfShares = 0,
                ContributionPercentage = 0m
            };
    
            OnHoldingAdded?.Invoke(newHolding); // ✅ ViewModel-to-ViewModel callback
    
            SearchText = string.Empty;
            FilteredStocks.Clear();
            SelectedStock = null;
            IsAddButtonClickable = false;
        }
    }
}
