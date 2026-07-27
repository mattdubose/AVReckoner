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
    }
}
