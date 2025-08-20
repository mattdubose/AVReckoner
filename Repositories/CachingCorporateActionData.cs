using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
    public class CachingCorporateActionData : ICorporateActionProvider
    {
        private readonly ICorporateActionProvider _source;
        private readonly int _windowSizeInDays;
        private DateTime _cachedStart = DateTime.MinValue;
        private DateTime _cachedEnd = DateTime.MinValue;
        private List<CorporateAction> _cachedActions = new();

        public CachingCorporateActionData(ICorporateActionProvider source, int windowSizeInDays = 365)
        {
            _source = source;
            _windowSizeInDays = windowSizeInDays;
        }

        private void EnsureCached(DateTime start, DateTime end)
        {
            if (start < _cachedStart || end > _cachedEnd)
            {
                var paddedStart = start.AddDays(-_windowSizeInDays / 2);
                var paddedEnd = end.AddDays(_windowSizeInDays / 2);
                _cachedActions = _source.GetDividends(paddedStart, paddedEnd); // or GetActions if preferred
                _cachedStart = paddedStart;
                _cachedEnd = paddedEnd;
            }
        }

        public List<CorporateAction> GetActions(DateTime date)
        {
            EnsureCached(date, date);
            return _cachedActions.Where(a => a.EffectiveDate == date).ToList();
        }

        public List<CorporateAction> GetDividends(DateTime startDate, DateTime endDate)
        {
            EnsureCached(startDate, endDate);
            return _cachedActions
                .Where(a => a.ActionType == "dividend" && a.EffectiveDate >= startDate && a.EffectiveDate <= endDate)
                .ToList();
        }

        public List<CorporateAction> GetDividend(DateTime date)
        {
            EnsureCached(date, date);
            return _cachedActions
                .Where(a => a.ActionType == "dividend" && a.EffectiveDate == date)
                .ToList();
        }

        public List<CorporateAction> GetSplits(DateTime date)
        {
            EnsureCached(date, date);
            return _cachedActions
                .Where(a => a.ActionType == "split" && a.EffectiveDate == date)
                .ToList();
        }
    }

}
