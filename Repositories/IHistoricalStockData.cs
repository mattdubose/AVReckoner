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
    /// <summary>True if at least one price row exists on or before <paramref name="date"/>, regardless of window/cache state.</summary>
    bool HasAnyDataOnOrBefore(DateTime date) => true;
  }
}
