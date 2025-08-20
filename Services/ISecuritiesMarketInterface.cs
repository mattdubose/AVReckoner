using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Services
{
public interface IMarketInterfaceErrors 
  {
    public MarketInterfaceErrors GetLastError();
    public void ClearErrors();
  }

  public enum MarketInterfaceErrors
  {
    NoError = 0,
    InvalidInterface = -1, 
    ParsingError = -2, 
    DateNotPresent = -3, 
    InvalidDate = -4/* means it's not going to be present. */, 
    ServiceUnavailable = -5,
  }
  public interface ISecuritiesMarketInterface : IMarketInterfaceErrors
  {
      decimal GetCurrentPrice(string securityID);
      public decimal BuyInDollars(string  securityID, decimal Dollars);
      public decimal BuyNumShares(string  securityID, decimal NumShares);
      public decimal SellInDollars(string securityID, decimal Dollars);
      public decimal SellNumShares(string securityID, decimal NumShares);
      public decimal GetLatestPrice(string tickerSymbol);
  }
}
