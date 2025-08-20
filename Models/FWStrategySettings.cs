using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Models
{
    public partial class FWStrategySettings
    {
        /* all percentages here are going to be positive, though they represent a negative (down from High) it's "down x percent", not at -x%" -- i may change this.*/

        public decimal TriggerToSell { get; set; } = .22m; // percentage from original to trigger a selloff.
        public decimal TriggerToBuyBack { get; set; } = .32m; // percentage from original to buy back in after a selloff. (
        public decimal TriggerToBuyRecovery { get; set; } = .18m;// percentage from original to buy back in after a selloff. 
        public int LookbackPeriod { get; set; } = 60; // days to look back for highs and lows.
        public int MaxDaysOutOfMarket { get; set; } = 30; // days, how long to wait before re-entering the market after a selloff regardless of the current price.
        public bool AllTimeEvaluation { get; set; } = false; // if true, evaluate all time highs and lows, otherwise just use
        public string TickerToEvaluate { get; set; } = string.Empty; // the ticker symbol of the asset that triggered the strategy.

    }
}
