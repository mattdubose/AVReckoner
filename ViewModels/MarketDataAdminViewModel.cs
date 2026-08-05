using AvReckoner;
using System.Text.RegularExpressions;

namespace Reckoner.ViewModels
{
    public partial class MarketDataAdminViewModel : BaseViewModel
    {
        private record SuggestedTickerDto(string Ticker, string Name);

        private readonly IMarketSecurityRepository _repo;
        private readonly MarketDataSyncService _sync;
        private readonly List<MarketSecurity> _allSecurities = new();

        // Securities table
        [ObservableProperty] ObservableCollection<MarketSecurityRecord> securities = new();
        [ObservableProperty] string mostRecentDate = "—";

        // Suggested tickers (checkbox list)
        [ObservableProperty] ObservableCollection<SuggestedTickerItem> suggestedTickers = new();

        // Update All
        [ObservableProperty] bool isUpdating;
        [ObservableProperty] string updateLog = string.Empty;
        [ObservableProperty] bool hasUpdateLog;
        [ObservableProperty] double updateProgress;
        [ObservableProperty] string updateStatusText = string.Empty;
        private int _updateTotalTickers;
        private int _updateCompletedTickers;

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
            LoadSuggestedTickers();
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

            SyncSuggestedTickerLoadedFlags();
        }

        private void SyncSuggestedTickerLoadedFlags()
        {
            var loadedTickers = _allSecurities
                .Select(s => s.TickerSymbol)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in SuggestedTickers)
                item.IsAlreadyLoaded = loadedTickers.Contains(item.Ticker);
        }

        private void LoadSuggestedTickers()
        {
            SuggestedTickers.Clear();
            var path = AppPaths.InstalledFile("Data/SuggestedTickers.json");
            if (!File.Exists(path)) return;

            var dtos = JsonSerializer.Deserialize<List<SuggestedTickerDto>>(
                File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var loadedTickers = _allSecurities
                .Select(s => s.TickerSymbol)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in dtos)
            {
                SuggestedTickers.Add(new SuggestedTickerItem
                {
                    Ticker = dto.Ticker,
                    Name = dto.Name,
                    IsAlreadyLoaded = loadedTickers.Contains(dto.Ticker)
                });
            }
        }

        [RelayCommand]
        async Task LoadSelectedSuggestedTickers()
        {
            var selected = SuggestedTickers.Where(t => t.IsSelected && !t.IsAlreadyLoaded).ToList();
            if (selected.Count == 0)
            {
                AppendLog("No suggested tickers selected.");
                return;
            }
            if (!_sync.IsConfigured)
            {
                AppendLog("fipy.exe not found. Build the fiPy project first.");
                return;
            }

            IsUpdating = true;
            _updateTotalTickers = selected.Count;
            _updateCompletedTickers = 0;
            UpdateProgress = 0;
            AppendLog($"Loading {selected.Count} suggested ticker(s) — {DateTime.Now:t}");

            var progress = new Progress<string>(line =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                    AppendLog(line);
            });

            foreach (var item in selected)
            {
                UpdateStatusText = $"Loading {item.Ticker}… ({_updateCompletedTickers + 1} of {_updateTotalTickers})";
                try
                {
                    var result = await _sync.AddTickerAsync(item.Ticker, progress);
                    if (result.Success)
                        item.IsAlreadyLoaded = true;
                    else
                        AppendLog($"Failed to load {item.Ticker}: {result.ErrorOutput.Trim()}");
                }
                catch (Exception ex)
                {
                    AppendLog($"Error loading {item.Ticker}: {ex.Message}");
                }
                _updateCompletedTickers++;
                UpdateProgress = Math.Min(100, (double)_updateCompletedTickers / _updateTotalTickers * 100);
            }

            LoadSecurities();
            UpdateStatusText = "Done loading suggested tickers.";
            AppendLog("✓ Done loading suggested tickers.");
            IsUpdating = false;
        }

        [RelayCommand]
        async Task UpdateAll()
        {
            if (!_sync.IsConfigured)
            {
                AppendLog("fipy.exe not found. Build the fiPy project first.");
                return;
            }
            IsUpdating = true;
            _updateTotalTickers = 0;
            _updateCompletedTickers = 0;
            UpdateProgress = 0;
            UpdateStatusText = "Starting update…";
            AppendLog($"Starting update — {DateTime.Now:t}");

            var progress = new Progress<string>(line =>
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                AppendLog(line);
                ParseUpdateAllProgressLine(line);
            });

            try
            {
                var result = await _sync.UpdateAllAsync(progress);
                AppendLog(result.Success ? "✓ Update complete." : $"Update finished with errors: {result.ErrorOutput.Trim()}");
                UpdateStatusText = result.Success ? "Update complete." : "Update finished with errors.";
                UpdateProgress = 100;
                LoadSecurities();
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
                UpdateStatusText = $"Error: {ex.Message}";
            }
            finally
            {
                IsUpdating = false;
            }
        }

        // fipy's "update" command streams: "INFO: Updating N ticker(s)", then per ticker
        // "INFO: Processing TICKER" followed eventually by SUCCESS/WARNING/ERROR for it.
        private void ParseUpdateAllProgressLine(string line)
        {
            if (line.StartsWith("INFO: Updating "))
            {
                var match = Regex.Match(line, @"INFO: Updating (\d+) ticker");
                if (match.Success)
                {
                    _updateTotalTickers = int.Parse(match.Groups[1].Value);
                    _updateCompletedTickers = 0;
                    UpdateProgress = 0;
                }
            }
            else if (line.StartsWith("INFO: Processing "))
            {
                var ticker = line["INFO: Processing ".Length..].Trim();
                UpdateStatusText = _updateTotalTickers > 0
                    ? $"Updating {ticker}… ({_updateCompletedTickers + 1} of {_updateTotalTickers})"
                    : $"Updating {ticker}…";
            }
            else if (line.StartsWith("SUCCESS:") || line.StartsWith("WARNING:") || line.StartsWith("ERROR:"))
            {
                _updateCompletedTickers++;
                if (_updateTotalTickers > 0)
                    UpdateProgress = Math.Min(100, (double)_updateCompletedTickers / _updateTotalTickers * 100);
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

            // A partial local match (e.g. "AAPL" while typing "AAPD") shouldn't hide
            // Search Online — only an empty box should.
            CanSearchOnline = !IsDownloading;
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
