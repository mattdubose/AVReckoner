using System;
using System.Collections.Generic;

namespace Reckoner.Models
{
    public class SimulationDayResult
    {
        public DateTime Date { get; set; }
        public Services.SuggestedAction Action { get; set; }
        /// Total account value: invested assets plus uninvested cash sitting on the sidelines.
        public decimal Balance { get; set; }
        public decimal Cash { get; set; }
        public decimal Contribution { get; set; }
        /// Raw close price and running high-water-mark, per ticker checked "Track in Export" —
        /// independent of the strategy's own internal high-tracking. Keyed by ticker symbol.
        public Dictionary<string, decimal> Closes { get; set; } = new();
        public Dictionary<string, decimal> AllTimeHighs { get; set; } = new();

        /// Actual shares held and price used, per real account holding — captured live during
        /// the simulation (as opposed to Closes/AllTimeHighs, which cover arbitrary reference
        /// tickers and are recomputed later at export time). Keyed by ticker symbol.
        public Dictionary<string, decimal> HoldingShares { get; set; } = new();
        public Dictionary<string, decimal> HoldingPrices { get; set; } = new();
        /// The strategy's own internal high-water-mark per ticker (FWInvestmentStrategy.EvaluationHighs) —
        /// the value actually compared against when deciding to sell, captured live for this day.
        public Dictionary<string, decimal> ReferenceHighs { get; set; } = new();
    }
}
