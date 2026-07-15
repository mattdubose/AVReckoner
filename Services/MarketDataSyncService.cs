namespace Reckoner.Services
{
    public record SyncResult(bool Success, List<string> OutputLines, string ErrorOutput);
    public record ExternalSecurityResult(string Ticker, string Name);

    public class MarketDataSyncService
    {
        private readonly string _dbPath;
        private readonly MarketDataSyncSettings _settings;

        public MarketDataSyncService(string dbPath, MarketDataSyncSettings settings)
        {
            _dbPath = dbPath;
            _settings = settings;
        }

        public bool IsConfigured => File.Exists(_settings.FiPyExePath);

        public Task<SyncResult> UpdateAllAsync(IProgress<string>? progress = null, CancellationToken ct = default)
            => RunAsync("update", null, progress, ct);

        public Task<SyncResult> AddTickerAsync(string ticker, IProgress<string>? progress = null, CancellationToken ct = default)
            => RunAsync("add", ticker.ToUpperInvariant(), progress, ct);

        public async Task<List<ExternalSecurityResult>> SearchExternalAsync(string query, CancellationToken ct = default)
        {
            var result = await RunAsync("search", query, null, ct);
            return result.OutputLines
                .Where(l => l.StartsWith("RESULT:"))
                .Select(l => l[7..].Split('|', 2))
                .Where(parts => parts.Length == 2)
                .Select(parts => new ExternalSecurityResult(parts[0].Trim(), parts[1].Trim()))
                .ToList();
        }

        private async Task<SyncResult> RunAsync(string command, string? arg, IProgress<string>? progress, CancellationToken ct)
        {
            var arguments = $"--db \"{_dbPath}\" {command}";
            if (arg != null) arguments += $" \"{arg}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _settings.FiPyExePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            var outputLines = new List<string>();

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Read stdout and stderr concurrently to avoid deadlocks when one buffer fills.
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            var stdoutTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync(ct);
                    if (line is null) continue;
                    outputLines.Add(line);
                    progress?.Report(line);
                }
            }, ct);

            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(ct);

            return new SyncResult(process.ExitCode == 0, outputLines, stderrTask.Result);
        }
    }
}
