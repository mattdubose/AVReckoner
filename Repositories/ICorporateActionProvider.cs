using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
    public interface ICorporateActionProvider
    {
        List<CorporateAction> GetActions(DateTime date);
        List<CorporateAction> GetDividends(DateTime startDate, DateTime endDate);
        List<CorporateAction> GetDividend(DateTime date);
        List<CorporateAction> GetSplits(DateTime date);
    }
}
