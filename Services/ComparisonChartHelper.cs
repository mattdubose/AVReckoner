using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Reckoner.Models;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace Reckoner.Services
{
    // One place to build the "compare several scenario runs on one chart" pieces — a balance
    // line per run plus shaded sections for every sell-off episode. Shared by AnalystViewModel
    // and ParameterSweepViewModel so a sweep's results compare exactly the way hand-built
    // scenarios already do, without duplicating the drawing logic in both places.
    public static class ComparisonChartHelper
    {
        private static readonly SKColor[] Palette =
        {
            SKColors.Black, SKColors.Green, SKColors.Blue, SKColors.Magenta, SKColors.Red,
            SKColors.DarkOrange, SKColors.Teal, SKColors.Purple,
        };

        public static ISeries[] BuildSeries(IReadOnlyList<(string Name, List<SimulationDayResult> Days)> rows)
        {
            return rows.Select((row, i) => (ISeries)new LineSeries<DateTimePoint>
            {
                Values = row.Days.Select(d => new DateTimePoint(d.Date, (double)d.Balance)).ToList(),
                Stroke = new SolidColorPaint(Palette[i % Palette.Length]) { StrokeThickness = 2 },
                Fill = null,
                GeometryFill = null,
                GeometryStroke = null,
                Name = row.Name,
            }).ToArray();
        }

        // Zoomed out over a long backtest, a short sell-off can be only a few pixels wide — pad
        // it out to a visible minimum width so it still reads as "eye popping" at any zoom level.
        public static RectangularSection[] BuildSections(
            IReadOnlyList<(string Name, List<SimulationDayResult> Days, List<StrategyEpisode> Episodes)> rows)
        {
            return rows
                .SelectMany(row => row.Episodes.Select(ep => (row, ep)))
                .Select(x =>
                {
                    DateTime start = x.ep.StartDate;
                    DateTime end = x.ep.EndDate ?? (x.row.Days.Count > 0 ? x.row.Days[^1].Date : start);
                    var padded = end - start < TimeSpan.FromDays(10)
                        ? (start.AddDays(-5), end.AddDays(5))
                        : (start, end);
                    return new RectangularSection
                    {
                        Xi = padded.Item1.Ticks,
                        Xj = padded.Item2.Ticks,
                        Fill = new SolidColorPaint(SKColors.Red.WithAlpha(110)),
                    };
                })
                .ToArray();
        }
    }
}
