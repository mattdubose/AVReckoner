using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
  public class CachingHistoricalStockData : IHistoricalStockData
  {
    private readonly IHistoricalStockData _dbSource;
    private readonly int _windowSizeInDays;
    private DateTime _cachedStart;
    private DateTime _cachedEnd;
    private Dictionary<DateTime, DailyEquityInfo> _cache = new();

    public CachingHistoricalStockData(IHistoricalStockData dbSource, int windowSizeInDays = 365)
    {
      _dbSource = dbSource;
      _windowSizeInDays = windowSizeInDays;
    }

    public DailyEquityInfo? GetInfo(DateTime date)
    {
      if (!_cache.ContainsKey(date))
      {
        // Cache miss: reload window
        _cachedStart = date.AddDays(-_windowSizeInDays / 2);
        _cachedEnd = date.AddDays(_windowSizeInDays / 2);
        var list = _dbSource.GetInfoBetweenDates(_cachedStart, _cachedEnd);
        _cache = list.ToDictionary(info => info.Date, info => info);
      }

      _cache.TryGetValue(date, out var result);
      return result;
    }

    public List<DailyEquityInfo> GetInfoBetweenDates(DateTime start, DateTime end)
    {
      // Simple version: if cache doesn't include entire range, load fresh
      if (start < _cachedStart || end > _cachedEnd)
      {
        _cachedStart = start;
        _cachedEnd = end;
        var list = _dbSource.GetInfoBetweenDates(start, end);
        _cache = list.ToDictionary(info => info.Date, info => info);
      }

      return _cache.Values
          .Where(d => d.Date >= start && d.Date <= end)
          .OrderBy(d => d.Date)
          .ToList();
    }

    public DailyEquityInfo? GetLatestDaysInfo(DateTime startDate, int maxLookback)
    {
      for (int i = 0; i < maxLookback; i++)
      {
        var date = startDate.AddDays(-i);
        var info = GetInfo(date);
        if (info != null) return info;
      }
      return null;
    }

    public List<DailyEquityInfo> GetLastXDays(DateTime endDate, int numberToGet)
    {
      return GetInfoBetweenDates(endDate.AddDays(-numberToGet), endDate);
    }

    public MarketInterfaceErrors GetLastError() => _dbSource.GetLastError();
    public void ClearErrors() => _dbSource.ClearErrors();
  }
}
