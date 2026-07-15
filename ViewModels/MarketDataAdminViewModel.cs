namespace Reckoner.ViewModels
{
    public partial class MarketDataAdminViewModel : BaseViewModel
    {
        private readonly IMarketSecurityRepository _repo;
        private readonly MarketDataSyncService _sync;
        private readonly List<MarketSecurity> _allSecurities = new();

        // Securities table
        [ObservableProperty] ObservableCollection<MarketSecurityRecord> securities = new();
        [ObservableProperty] string mostRecentDate = "—";

        // Update All
        [ObservableProperty] bool isUpdating;
        [ObservableProperty] string updateLog = string.Empty;
        [ObservableProperty] bool hasUpdateLog;

        // Add / Search Online (same pattern as AccountHoldingsPage)
        [ObservableProperty] string searchText = string.Empty;
        [ObservableProperty] ObservableCollection<MarketSecurity> filteredStocks = new();
        [ObservableProperty] bool canSearchOnline;
        [ObservableProperty] ObservableCollection<ExternalSecurityResult> externalResults = new();
        [ObservableProperty] ExternalSecurityResult? selectedExternalResult;
        [ObservableProperty] bool hasExternalResults;
        [ObservableProperty] bool canDownload;
        [ObservableProperty] bool isDownloading;
        [ObservableProperty] string downloadStatus = string.Empty;
        [ObservableProperty] bool hasStatusMessage;

        public MarketDataAdminViewModel(
            AppShellService appShell,
            IMarketSecurityRepository repo,
            MarketDataSyncService sync) : base(appShell)
        {
            _repo = repo;
            _sync = sync;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            LoadSecurities();
        }

        private void LoadSecurities()
        {
            var records = _repo.GetAllWithDetails();
            Securities.Clear();
            _allSecurities.Clear();
            foreach (var r in records)
            {
                Securities.Add(r);
                _allSecurities.Add(r);
            }

            var latest = records
                .Where(r => r.LatestDate.HasValue)
                .Select(r => r.LatestDate!.Value)
                .DefaultIfEmpty()
                .Max();

            MostRecentDate = latest == default
                ? "No data yet"
                : latest.ToString("MMMM d, yyyy");
        }

        [RelayCommand]
        void GoBack() => _appShellService.Navigation.GoBack();

        [RelayCommand]
        async Task UpdateAll()
        {
            if (!_sync.IsConfigured)
            {
                AppendLog("fipy.exe not found. Build the fiPy project first.");
                return;
            }
            IsUpdating = true;
            AppendLog($"Starting update — {DateTime.Now:t}");

            var progress = new Progress<string>(line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                    AppendLog(line);
            });

            try
            {
                var result = await _sync.UpdateAllAsync(progress);
                AppendLog(result.Success ? "✓ Update complete." : $"Update finished with errors: {result.ErrorOutput.Trim()}");
                LoadSecurities();
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
            }
            finally
            {
                IsUpdating = false;
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ExternalResults.Clear();
            HasExternalResults = false;
            SelectedExternalResult = null;
            CanDownload = false;
            SetStatus(string.Empty);

            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredStocks.Clear();
                CanSearchOnline = false;
                return;
            }

            var matches = _allSecurities
                .Where(s => s.TickerSymbol.Contains(value, StringComparison.OrdinalIgnoreCase)
                         || s.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredStocks.Clear();
            foreach (var m in matches)
                FilteredStocks.Add(m);

            CanSearchOnline = FilteredStocks.Count == 0 && !IsDownloading;
        }

        partial void OnSelectedExternalResultChanged(ExternalSecurityResult? value)
        {
            CanDownload = value != null && !IsDownloading;
        }

        [RelayCommand]
        async Task SearchOnline()
        {
            if (!_sync.IsConfigured)
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
                var results = await _sync.SearchExternalAsync(SearchText);
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
            if (!_sync.IsConfigured)
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
                    line.StartsWith("ERROR:")    || line.StartsWith("WARNING:"))
                    SetStatus(line[(line.IndexOf(':') + 1)..].Trim());
            });

            try
            {
                var result = await _sync.AddTickerAsync(ticker, progress);
                if (result.Success)
                {
                    ExternalResults.Clear();
                    HasExternalResults = false;
                    SelectedExternalResult = null;
                    SearchText = string.Empty;
                    SetStatus(string.Empty);
                    LoadSecurities();
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

        private void AppendLog(string line)
        {
            UpdateLog = string.IsNullOrEmpty(UpdateLog) ? line : UpdateLog + "\n" + line;
            HasUpdateLog = true;
        }

        private void SetStatus(string message)
        {
            DownloadStatus = message;
            HasStatusMessage = !string.IsNullOrEmpty(message);
        }
    }
}
