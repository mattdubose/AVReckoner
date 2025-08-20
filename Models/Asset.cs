using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Models
{
  public enum AssetType
  {
    MarketSecurity,
    RealEstate
  }
  public interface IAsset
  {
    public AssetType AssetType { get; }
    public string ID { get; }
  }
  public abstract class Asset : IAsset
  {
    public abstract string ID { get; }
    public string Name { get; set; } = string.Empty;
    public abstract AssetType AssetType { get; }
  }
}
