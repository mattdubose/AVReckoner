using Reckoner.Repositories;
using Reckoner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Services
{
    public class HistoricalBasedMarketInterface : ISecuritiesMarketInterface
  {
    IDateProvider? _dateTimeProvider  = null;
    IHistoricalStockData _historicalDataIf;
    MarketInterfaceErrors _marketInterfaceErrors;

    public HistoricalBasedMarketInterface(IHistoricalStockData historicalStockData) 
    {
      this._historicalDataIf = historicalStockData;
    }

    private DailyEquityInfo? GetTodaysStockData() 
    {
      DateTime today = DateTimeService.GetInstance.GetCurrentDate();
      if (_dateTimeProvider != null) 
      {
        today = _dateTimeProvider.GetCurrentDate();
      }
      DailyEquityInfo? todaysInfo = _historicalDataIf.GetInfo(today);
      return todaysInfo;
    }
    public decimal BuyInDollars(string securityID, decimal Dollars)
    {
      throw new NotImplementedException();
    }

    public decimal BuyNumShares(string securityID, decimal NumShares)
    {
      throw new NotImplementedException();
    }

    
    public decimal SellInDollars(string securityID, decimal Dollars)
    {
      throw new NotImplementedException();
    }

    public decimal SellNumShares(string securityID, decimal NumShares)
    {
      throw new NotImplementedException();
    }
    public DailyEquityInfo? GetRecentStockData(int maxDays) 
    {
      DateTime today = DateTimeService.GetInstance.GetCurrentDate();
      if (_dateTimeProvider != null)
      {
        today = _dateTimeProvider.GetCurrentDate();
      }
      DateTime firstDay = today.AddDays(-maxDays);
      
      List<DailyEquityInfo> results = _historicalDataIf.GetLastXDays(today,maxDays);
      if (results.Count > 0)
      {
        /* list should be in descending order, so this is always first. */
          return results[0];
      }
      return null;

    }

    public decimal GetCurrentPrice(string securityID)
    {

      DailyEquityInfo? stockData = GetTodaysStockData();
      if (stockData != null && stockData.Close.HasValue)
      {
        return (decimal)stockData.Close;
      }
      // No exact-date row for today (weekend, holiday, any gap) — fall back to the same
      // most-recent-available-close lookback GetLatestPrice uses, instead of returning
      // GetLastError()'s error code as if it were a real price (it used to be cast straight
      // into the return value here, e.g. -3.0m for DateNotPresent).
      return GetLatestPrice(securityID);
    }

    public decimal GetLatestPrice(string tickerSymbol)
    {
      DateTime today = DateTimeService.GetInstance.GetCurrentDate();
      if (_dateTimeProvider != null)
      {
        today = _dateTimeProvider.GetCurrentDate();
      }

      DailyEquityInfo? stockData = _historicalDataIf.GetLatestDaysInfo(today, 300);
      if (stockData == null || !stockData.Close.HasValue)
      {
        // Genuinely no price within the lookback window — 0 is the "no valid price" sentinel
        // every caller already checks for (asset.GetCurrentPrice() <= 0), unlike an arbitrary
        // error-code value that happens to also be <= 0.
        return 0m;
      }
      return (decimal)stockData.Close;

    }

    public void PreloadRange(string tickerSymbol, DateTime start, DateTime end)
    {
      // A single GetInfoBetweenDates call fills the caching layer for the entire range
      _historicalDataIf.GetInfoBetweenDates(start, end);
    }

    public bool HasDataOnOrBefore(DateTime date) => _historicalDataIf.HasAnyDataOnOrBefore(date);

    public MarketInterfaceErrors GetLastError()
    {
      throw new NotImplementedException();
    }

    public void ClearErrors()
    {
      throw new NotImplementedException();
    }
  }
}
