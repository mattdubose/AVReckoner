using System;
using System.Text.Json.Serialization;

namespace Reckoner.Models
{
    // The four buckets from "Balancing your Accounts" — Other covers anything tracked but not
    // counted in the "liquid assets" grand total (e.g. home equity).
    public enum NetWorthCategory
    {
        Cash,
        TaxableInvested,
        PreTax,
        TaxFree,
        Other,
    }

    // A named household account tracked for net worth purposes (checking, 401k, Roth IRA, home
    // equity, ...) — distinct from Models.Account, which represents a market-securities
    // investment portfolio elsewhere in this app. This is just a label + category; balances live
    // separately in NetWorthBalanceEntry so history accumulates instead of re-typing the whole
    // account list every period, which is how the source spreadsheet was actually being used.
    public class NetWorthAccount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize/deserialize as a string
        public NetWorthCategory Category { get; set; }
    }

    // One account's balance as of one date. Multiple entries per account accumulate into history.
    public class NetWorthBalanceEntry
    {
        public string AccountId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Balance { get; set; }
    }

    // The emergency-fund threshold used to compute "excess cash" (Cash total minus this) —
    // tracked over time the same way account balances are, since it's just another number that
    // can change per period.
    public class EmergencyFundThresholdEntry
    {
        public int ClientId { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
