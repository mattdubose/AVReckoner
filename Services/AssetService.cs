using Reckoner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Services
{
  public class AssetService : SecurityHolding // This could be renamed in the future to LocalAssetService - it does the buying and selling here.
  {
    ISecuritiesMarketInterface _marketInterface;
    ICorporateActionProvider _corpActionRepo;
    Dictionary<DateTime, decimal> _dividends = new Dictionary<DateTime, decimal>();
    public AssetService(ISecuritiesMarketInterface marketInterface, ICorporateActionProvider corporateActionProvider, SecurityHolding security) : base(security)
    {
        _marketInterface = marketInterface;
        _corpActionRepo  = corporateActionProvider;
    }
    public decimal BuyInDollars(decimal Dollars)
    {
      decimal curPrice = _marketInterface.GetCurrentPrice(TickerSymbol);
      if (curPrice <= 0)
      {

        Debug.WriteLine($"No valid price for {TickerSymbol} - cannot inititate buy.");
        return 0;
      }
      decimal numShares = Dollars / curPrice;
      NumberOfShares += numShares;
      return numShares;
    }
    public decimal BuyNumShares(decimal NumShares)
    {
      decimal curPrice = _marketInterface.GetCurrentPrice(TickerSymbol);
      if (curPrice <= 0)
      {
        Debug.WriteLine($"No valid price for {TickerSymbol} - cannot inititate buy.");
        return 0;
      }
      decimal cost = NumShares * curPrice;
      NumberOfShares += NumShares;
      return cost;
    }

    public decimal SellInDollars(decimal Dollars)
    {
      decimal curPrice = _marketInterface.GetCurrentPrice(TickerSymbol);
      if (curPrice <= 0)
      {
        Debug.WriteLine($"No valid price for {TickerSymbol} - cannot inititate sell.");
        return 0;
      }
      decimal numShares = Dollars / curPrice;
      NumberOfShares -= numShares;
      return numShares;
    }

    public decimal SellNumShares(decimal NumShares)
    {
      decimal curPrice = _marketInterface.GetCurrentPrice(TickerSymbol);
      if (curPrice <= 0)
      {
        Debug.WriteLine($"No valid price for {TickerSymbol} - cannot inititate sell.");
        return 0;
      }

      decimal price = NumShares * curPrice;
      NumberOfShares -= NumShares;
      return price;
    }

    public decimal DividendReinvestment(DateTime today)
    {
      if (_dividends.TryGetValue(today, out decimal value))
      {
        Debug.WriteLine($"Got dividend for Date: {today} ");
        return BuyInDollars(value);
      }
      return 0;
    }
    public decimal GetTotalDividendAmount(DateTime today) 
    {
            decimal perShareDividend = 0;
            List<CorporateAction> dividends = _corpActionRepo.GetDividend(today);
            foreach (var dividend in dividends)
            {
                perShareDividend += dividend.ActionValue;
            }
            return (perShareDividend * NumberOfShares);
    }
    public void HandleSplit(DateTime today)
    {
        List<CorporateAction> splits= _corpActionRepo.GetSplits(today);
        foreach (var spl in splits)
        {
          NumberOfShares*=spl.ActionValue;
        }
    }

    internal decimal GetCurrentPrice()
    {
      return  _marketInterface.GetCurrentPrice(TickerSymbol);
    }

    internal decimal GetLatestPrice()
    {
      return _marketInterface.GetLatestPrice(TickerSymbol); 
    }
  }
}
