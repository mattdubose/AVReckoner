using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Services
{
    public enum StrategyState
    {
        Normal = 0,
        Selloff = 1,
    }
    public enum InvestmentStrategyType
    {
        Conservative,
        Balanced,
        Aggressive
    }
    public enum SuggestedAction
    {
        Buy,
        Sell,
        Hold
    }
    public class FWInvestmentStrategy 
    {
        /* this is not the way to do this, but I will need to go back and change interfaces, and all implementations to give me an 
         * alltime low and a high for each asset, as well as a way to lookback and do a low/high from a period of Time. - right now, 
         * just do this highsForEvaluation so we can get a proof of concept. 
         */
        Dictionary<string, decimal> highsForEvaluation = new Dictionary<string, decimal>();
        private StrategyState _curState;
        private bool disableEvaluation = false;
        private List<AssetService> _assets;
        private ISecuritiesMarketInterface _marketInterface;

        private FWStrategySettings? _configData = null;
        public decimal? TriggerBuyBackPrice { get; private set; } // TriggerToBuy * TriggeredPrice, the price at which we will buy in after a selloff.
        public decimal? RecoveryBuyPrice { get; private set; } // TriggerToBuyRecovery * TriggeredPrice, the price at which we will buy back in after a selloff.
        public string TickerTriggeredOn { get; private set; } = string.Empty; // the ticker symbol of the asset that triggered the strategy.
        public decimal? TriggeredPrice { get; private set; }  // price at which the strategy was triggered.
        public DateTime? TriggeredDate { get; private set; } = DateTime.MinValue; // date at which the strategy was triggered.

        public FWInvestmentStrategy(List<AssetService> assets, object StrategyConfig)
        {
           if (StrategyConfig is FWStrategySettings settings)
            {
                _configData = settings;
            }
            else
            {
                Console.WriteLine("Settings passed into FWInvestmentStrategy is not a FWStrategySetting - it' ssomething different.");
                _configData = new FWStrategySettings();
            }
            _assets = assets;
        }

        private SuggestedAction EvaluateForBuyback ()
        {
            SuggestedAction retState = SuggestedAction.Sell;
            if (TriggeredPrice == null || TriggeredDate == null || _curState != StrategyState.Selloff)
            { /* WMD ### show this - this is a point that we could expand on. */
                // If we haven't triggered a selloff, we can't evaluate for buyback.
                return SuggestedAction.Sell;// SuggestedAction.Hold; // No trigger, so we can't evaluate for buyback.
            }
            if  (TriggeredDate.HasValue && TriggeredDate.Value.AddDays(_configData.MaxDaysOutOfMarket) >= DateTime.Now)
            {
                // If we have been out of the market for too long, we will buy back regardless of price.
                Debug.WriteLine("Buying back because I've been out of the market too long.  it's stable I suppose, or low.");
                retState = SuggestedAction.Buy;
            }
            else 
            {
                foreach (var asset in _assets)
                {
//                    if (asset.TickerSymbol != _configData.TickerToEvaluate) continue;

                    var currentPrice = asset.GetLatestPrice();
                    if (currentPrice <= TriggerBuyBackPrice)
                    {
                        Debug.WriteLine("Hit ahe lower price, may not be the bottom, but I just gained a nice delta.");
                        retState = SuggestedAction.Buy; // Current price is at or below the buyback trigger price.
                    }
                    if (currentPrice >= RecoveryBuyPrice)
                    {
                        Debug.WriteLine("It's going bak up, go ahead and buy!");
                        retState = SuggestedAction.Buy; // Current price is at or below the buyback trigger price.
                    }
                }
            }
            if (retState == SuggestedAction.Buy)
            {
                _curState = StrategyState.Normal;
                TickerTriggeredOn = string.Empty;
                TriggeredDate = null;
                TriggeredPrice = null;
                disableEvaluation = true;

            }
            return retState;
        }
        public SuggestedAction DetermineActionOnAccount()
        {                    /* start temporary code */
            SaveNewHigh();
            /* end temporary code */

            if (_curState == StrategyState.Selloff)
            {
                // If we are in a selloff state, we will evaluate all assets to determine if we should buy back in.
                return EvaluateForBuyback();
            }
            else
            {
                foreach (var asset in _assets)
                {
                    if (asset.ID != _configData.TickerToEvaluate)
                        continue;
                    var action = EvaluateForSelloff(asset);
                    if (action != SuggestedAction.Hold)
                    {
                        return action; // If any asset suggests an action other than hold, we will take that action.
                    }
                }
            }
            return SuggestedAction.Buy;

        }
        
        private void SaveNewHigh()
        {
            foreach (var asset in _assets)
            {
                decimal curPrice = asset.GetLatestPrice();
                if (highsForEvaluation.ContainsKey(asset.TickerSymbol))
                {
                    if (curPrice > highsForEvaluation[asset.TickerSymbol])
                    {
                        highsForEvaluation[asset.TickerSymbol] = curPrice; // Update the high for this asset.
                        disableEvaluation = false; // Reset the disableEvaluation flag since we have a new high.
                    }
                }
                else
                {
                    highsForEvaluation.Add(asset.TickerSymbol, curPrice); // Add the asset to the highs for evaluation.
                }
            }
        }

        private SuggestedAction EvaluateForSelloff(AssetService asset)
        {
            if (disableEvaluation) return SuggestedAction.Hold;
            if ( string.IsNullOrEmpty(asset.TickerSymbol))
            {
                return SuggestedAction.Hold; // No ticker symbol provided, so we can't evaluate for selloff.
            }
            if (_curState == StrategyState.Selloff)
            {
                // If we are already in a selloff state, we will not evaluate for selloff again.
                return SuggestedAction.Hold; // We are already in a selloff state, so we will not evaluate for selloff again.
            }
            decimal priceForSelloff = highsForEvaluation[asset.TickerSymbol] * (1 - _configData.TriggerToSell); // Calculate the price at which we will trigger a selloff.
            var currentPrice = asset.GetLatestPrice();
            if (currentPrice <= priceForSelloff)
            {
                decimal _highWaterMark = highsForEvaluation[asset.TickerSymbol];
                _curState = StrategyState.Selloff;
                TriggeredDate = DateTimeService.GetInstance.GetCurrentDate();
                TriggeredPrice = currentPrice;
                TriggerBuyBackPrice = _highWaterMark * (1 - _configData.TriggerToBuyBack); // Calculate the price at which we will buy back in after a selloff.
                TickerTriggeredOn = asset.TickerSymbol;
                RecoveryBuyPrice = _highWaterMark * (1 - _configData.TriggerToBuyRecovery); // Calculate the price at which we will buy back in after a selloff.
                return SuggestedAction.Sell; 
            }
            else return SuggestedAction.Hold; // Current price is above the selloff trigger price, so we will hold.
        }

        public SuggestedAction DetermineActionOnAsset(string TickerSymbol)
        {
            throw new NotImplementedException("This method is not implemented yet. It should analyze the specific asset and suggest actions based on the current market conditions.");
            // This method would analyze the specific asset and suggest actions based on the current market conditions.
            // For simplicity, let's assume it always suggests to execute the strategy.
            return SuggestedAction.Buy; // Example action
        }

    }
}
