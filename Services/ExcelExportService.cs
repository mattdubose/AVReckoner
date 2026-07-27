using ClosedXML.Excel;
using Reckoner.Models;
using System.Collections.Generic;

namespace Reckoner.Services
{
    public class SimulationRunExport
    {
        public string Name { get; set; }
        public List<SimulationDayResult> Days { get; set; }
        /// Tickers checked "Track in Export" for this run — drives the Close/All-Time-High column pairs.
        public List<string> TrackedTickers { get; set; } = new();
    }

    public static class ExcelExportService
    {
        private const int FixedColumnsPerRun = 5; // Date, Action, Balance, Cash, Contribution
        private const int ColumnsPerTracker = 2; // Close, All-Time High
        private const int BlankColumnsBetweenRuns = 2;

        public static void ExportRuns(string filePath, IReadOnlyList<SimulationRunExport> runs)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Simulations");

            int startColumn = 1;
            foreach (var run in runs)
            {
                int columnsUsed = WriteRun(ws, run, startColumn);
                startColumn += columnsUsed + BlankColumnsBetweenRuns;
            }

            ws.SheetView.FreezeRows(2);
            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private static int WriteRun(IXLWorksheet ws, SimulationRunExport run, int startColumn)
        {
            int columnCount = FixedColumnsPerRun + ColumnsPerTracker * run.TrackedTickers.Count;

            var nameCell = ws.Cell(1, startColumn);
            nameCell.Value = run.Name;
            nameCell.Style.Font.Bold = true;
            ws.Range(1, startColumn, 1, startColumn + columnCount - 1).Merge();

            ws.Cell(2, startColumn).Value = "Date";
            ws.Cell(2, startColumn + 1).Value = "Action";
            ws.Cell(2, startColumn + 2).Value = "Balance";
            ws.Cell(2, startColumn + 3).Value = "Cash";
            ws.Cell(2, startColumn + 4).Value = "Contribution";
            for (int t = 0; t < run.TrackedTickers.Count; t++)
            {
                string ticker = run.TrackedTickers[t];
                int col = startColumn + FixedColumnsPerRun + t * ColumnsPerTracker;
                ws.Cell(2, col).Value = $"{ticker} Close";
                ws.Cell(2, col + 1).Value = $"{ticker} All-Time High";
            }
            ws.Range(2, startColumn, 2, startColumn + columnCount - 1).Style.Font.Bold = true;

            int row = 3;
            foreach (var day in run.Days)
            {
                ws.Cell(row, startColumn).Value = day.Date;
                ws.Cell(row, startColumn).Style.DateFormat.Format = "yyyy-MM-dd";
                ws.Cell(row, startColumn + 1).Value = day.Action.ToString();
                ws.Cell(row, startColumn + 2).Value = day.Balance;
                ws.Cell(row, startColumn + 2).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, startColumn + 3).Value = day.Cash;
                ws.Cell(row, startColumn + 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, startColumn + 4).Value = day.Contribution;
                ws.Cell(row, startColumn + 4).Style.NumberFormat.Format = "$#,##0.00";

                for (int t = 0; t < run.TrackedTickers.Count; t++)
                {
                    string ticker = run.TrackedTickers[t];
                    int col = startColumn + FixedColumnsPerRun + t * ColumnsPerTracker;
                    if (day.Closes.TryGetValue(ticker, out var close))
                    {
                        ws.Cell(row, col).Value = close;
                        ws.Cell(row, col).Style.NumberFormat.Format = "$#,##0.00";
                    }
                    if (day.AllTimeHighs.TryGetValue(ticker, out var high))
                    {
                        ws.Cell(row, col + 1).Value = high;
                        ws.Cell(row, col + 1).Style.NumberFormat.Format = "$#,##0.00";
                    }
                }
                row++;
            }

            return columnCount;
        }
    }
}
