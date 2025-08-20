using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
  public interface IHistoricalStockData : IMarketInterfaceErrors
  {
    DailyEquityInfo? GetInfo(DateTime dateTime);
    List<DailyEquityInfo> GetInfoBetweenDates(DateTime startDate, DateTime endDate);
    List<DailyEquityInfo> GetLastXDays(DateTime endDate, int NumberToGet);
    DailyEquityInfo? GetLatestDaysInfo(DateTime startDate, int MaxLookback);
  }
}
