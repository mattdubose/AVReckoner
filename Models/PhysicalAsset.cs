using System;

namespace Reckoner.Models
{
    // A physical/illiquid asset tracked for net worth purposes (house, vehicles, jewelry, ...).
    // Same shape as NetWorthAccount/NetWorthBalanceEntry, but also carries Debt and its interest
    // rate — unlike a bank/brokerage account, what a physical asset is actually "worth" to the
    // household is Value minus whatever's owed against it, not just Value alone.
    public class PhysicalAsset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // One asset's value/debt as of one date. InterestRate is a fraction (0.065, not 6.5).
    public class PhysicalAssetValueEntry
    {
        public string AssetId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public decimal Debt { get; set; }
        public decimal InterestRate { get; set; }
    }
}
