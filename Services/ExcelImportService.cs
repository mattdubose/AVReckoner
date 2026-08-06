using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace Reckoner.Services
{
    public static class ExcelImportService
    {
        // 1-6 letters, optional ".XX" exchange suffix (e.g. "APC.DE") — matches fipy's own
        // search results and covers the vast majority of real ticker symbols.
        private static readonly Regex TickerPattern = new(@"^[A-Z]{1,6}(\.[A-Z]{1,3})?$", RegexOptions.Compiled);

        private static readonly HashSet<string> HeaderWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "ticker", "tickers", "symbol", "symbols", "stock", "stocks", "ticker symbol"
        };

        /// Reads the first column of the first worksheet and returns any values that look like
        /// stock tickers, in file order, deduplicated. Skips blank cells and an optional header
        /// row (e.g. "Ticker" / "Symbol").
        public static List<string> ImportTickers(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            var tickers = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in ws.Column(1).CellsUsed())
            {
                var raw = cell.GetString().Trim();
                if (raw.Length == 0 || HeaderWords.Contains(raw)) continue;

                var candidate = raw.ToUpperInvariant();
                if (!TickerPattern.IsMatch(candidate)) continue;

                if (seen.Add(candidate))
                    tickers.Add(candidate);
            }

            return tickers;
        }
    }
}
