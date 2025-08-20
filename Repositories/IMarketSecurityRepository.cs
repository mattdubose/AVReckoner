using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
    public interface IMarketSecurityRepository
    {
        public List<MarketSecurity> GetAll();
        public List<MarketSecurity> SearchByTickerOrName(string input);
        public List<MarketSecurity> SearchByTicker(string input);
    }
}
