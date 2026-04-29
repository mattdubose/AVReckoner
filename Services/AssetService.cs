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

    // Per-day price cache — avoids redundant DB/cache lookups within the same simulated day
    private DateTime _priceCacheDate = DateTime.MinValue;
    private decimal _cachedCurrentPrice = 0;
    private decimal _cachedLatestPrice = 0;

    public AssetService(ISecuritiesMarketInterface marketInterface, ICorporateActionProvider corporateActionProvider, SecurityHolding security) : base(security)
    {
        _marketInterface = marketInterface;
        _corpActionRepo  = corporateActionProvider;
    }

    /// <summary>
    /// Pre-warms the price and corporate action caches for the full simulation date range.
    /// Call once before starting the simulation loop to avoid repeated DB round-trips.
    /// </summary>
    public void Preload(DateTime start, DateTime end)
    {
        // Triggers a single range query that fills the caching layers completely
        _marketInterface.PreloadRange(TickerSymbol, start, end);
        _corpActionRepo.GetDividends(start, end); // warms the corp action cache
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
      var today = DateTimeService.GetInstance.GetCurrentDate();
      if (today == _priceCacheDate && _cachedCurrentPrice > 0)
        return _cachedCurrentPrice;

      var price = _marketInterface.GetCurrentPrice(TickerSymbol);
      if (price > 0)
      {
        _priceCacheDate = today;
        _cachedCurrentPrice = price;
      }
      return price;
    }

    internal decimal GetLatestPrice()
    {
      var today = DateTimeService.GetInstance.GetCurrentDate();
      if (today == _priceCacheDate && _cachedLatestPrice > 0)
        return _cachedLatestPrice;

      var price = _marketInterface.GetLatestPrice(TickerSymbol);
      if (price > 0)
      {
        _priceCacheDate = today;
        _cachedLatestPrice = price;
      }
      return price;
    }
  }
}
