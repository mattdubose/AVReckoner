using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Models
{
  public class CorporateAction
  {
    public int Id { get; set; }
    public string Ticker { get; set; } = string.Empty;  // Foreign key to market_securities
    public string ActionType { get; set; } = string.Empty;  // e.g., "split", "dividend"
    public decimal ActionValue { get; set; }  // Ratio or dividend amount
    public DateTime EffectiveDate { get; set; }
  }
}
