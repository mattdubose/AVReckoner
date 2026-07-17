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
      // If the whole lookback range is within cache, use O(1) dictionary lookups
      // instead of triggering DB round-trips. Checking only startDate here (and not
      // the earliest date the loop below can reach) let the loop walk past
      // _cachedStart and silently miss cached-but-unindexed-that-far-back data,
      // returning null instead of falling back to the DB.
      var earliestNeeded = startDate.AddDays(-(maxLookback - 1));
      if (earliestNeeded >= _cachedStart && startDate <= _cachedEnd)
      {
        for (int i = 0; i < maxLookback; i++)
        {
          if (_cache.TryGetValue(startDate.AddDays(-i), out var cached))
            return cached;
        }
        return null;
      }
      // Outside cache window — fall back to DB
      for (int i = 0; i < maxLookback; i++)
      {
        var info = GetInfo(startDate.AddDays(-i));
        if (info != null) return info;
      }
      return null;
    }

    public List<DailyEquityInfo> GetLastXDays(DateTime endDate, int numberToGet)
    {
      var start = endDate.AddDays(-numberToGet);
      if (start >= _cachedStart && endDate <= _cachedEnd)
      {
        return _cache.Values
            .Where(d => d.Date >= start && d.Date <= endDate)
            .OrderByDescending(d => d.Date)
            .Take(numberToGet)
            .ToList();
      }
      return GetInfoBetweenDates(start, endDate);
    }

    public MarketInterfaceErrors GetLastError() => _dbSource.GetLastError();
    public void ClearErrors() => _dbSource.ClearErrors();
  }
}
