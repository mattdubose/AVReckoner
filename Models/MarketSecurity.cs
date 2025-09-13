using System;
using System.Text.Json.Serialization;

namespace Reckoner.Models
{
  public class MarketSecurity : Asset
  {
        [ExplicitConstructor]
    public MarketSecurity(string symbol, string name)
    {
      TickerSymbol = symbol;
      Name = name;
    }
    public MarketSecurity(string symbol)
    {
      TickerSymbol = symbol;
      Name = string.Empty;
    }
    public MarketSecurity(MarketSecurity security)
    {
      TickerSymbol = security.TickerSymbol;
      Name = security.Name;
    }
    public MarketSecurity()
    {
      TickerSymbol = string.Empty;
      Name = string.Empty;
    }

    public string TickerSymbol { get; set; } = string.Empty;

    // Implement ID property to return TickerSymbol for MarketSecurity
    public override string ID => TickerSymbol;

    public override AssetType AssetType => AssetType.MarketSecurity;

  }

  public class MarketSecurityRecord : MarketSecurity
    {
        public DateTime? EarliestDate { get; set; }
        public DateTime? LatestDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class SecurityHolding : MarketSecurity
  {
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? FirstAcquisitionDate { get; set; } /* reset when quantity goes to zero */
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? RecentAcquisitionDate { get; set; }
    public decimal NumberOfShares { get; set; } = 0;
    public decimal? ContributionPercentage { get; set; } //  0-1 
    public decimal? DistributionPercentage { get; set; } //  0-1 
    /// <summary>
    /// Two-way UI property in the 0–100 range.
    /// Mark JsonIgnore so you don’t accidentally double-serialize it.
    /// </summary>
    [JsonIgnore]
    public decimal ContributionPercentageX100
    {
        get => (ContributionPercentage ?? 0m) * 100m;
        set => ContributionPercentage = value / 100m;
    }

    [JsonIgnore]
    public decimal DistributionPercentageX100
    {
        get => (DistributionPercentage ?? 0m) * 100m;
        set => DistributionPercentage = value / 100m;
    }

        public SecurityHolding() : base(string.Empty) // Default base constructor
    { /* exists for json reasons. */
    }
    public SecurityHolding(SecurityHolding passedHolding) : base(passedHolding.TickerSymbol, passedHolding.Name)
    {
      ContributionPercentage = passedHolding.ContributionPercentage;
      DistributionPercentage = passedHolding.DistributionPercentage;
      FirstAcquisitionDate = passedHolding.FirstAcquisitionDate;
      RecentAcquisitionDate = passedHolding.RecentAcquisitionDate;
      NumberOfShares = passedHolding.NumberOfShares;
    }
    public SecurityHolding(string symbol, string name) : base(symbol, name)
    {
      ContributionPercentage = 0;
      DistributionPercentage = 0;
    }
    public SecurityHolding(MarketSecurity security) : base(security)
    {
      ContributionPercentage = 0;
      DistributionPercentage = 0;
    }
    SecurityHolding(string symbol, decimal? contributionPercentage, decimal? distributionPercentage) : base(symbol)
    {
      ContributionPercentage = contributionPercentage;
      DistributionPercentage = distributionPercentage;
    }

  }

    public class DailyEquityInfo
    {
        [JsonConverter(typeof(DateOnlyJsonConverter))]
        public DateTime Date { get; set; }
        public decimal? High { get; set; }
        public decimal? Low { get; set; }
        public decimal? Open { get; set; }
        public decimal? Close { get; set; }
        public decimal? OverallHigh { get; set; }
        public long Volume { get; set; } = 0; // Default to 0 if not provided
    }
}
