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
      return (decimal)_historicalDataIf.GetLastError();
    }

    public decimal GetLatestPrice(string tickerSymbol)
    {
      DateTime today = DateTimeService.GetInstance.GetCurrentDate();
      if (_dateTimeProvider != null)
      {
        today = _dateTimeProvider.GetCurrentDate();
      }

      DailyEquityInfo? stockData = _historicalDataIf.GetLatestDaysInfo(today, 300);
      if (stockData == null)
      {
        return (decimal)_historicalDataIf.GetLastError();
      }
      return (decimal)stockData.Close;
      
    }

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
