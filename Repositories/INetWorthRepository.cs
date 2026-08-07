using System;
using System.Collections.Generic;
using Reckoner.Models;

namespace Reckoner.Repositories
{
    public interface INetWorthRepository
    {
        List<NetWorthAccount> GetAccounts(int clientId);
        List<NetWorthBalanceEntry> GetBalanceEntries(int clientId);
        List<EmergencyFundThresholdEntry> GetEmergencyFundThresholds(int clientId);

        NetWorthAccount AddAccount(int clientId, string name, NetWorthCategory category);
        void RemoveAccount(string accountId);
        void SetBalance(string accountId, DateTime date, decimal balance);
        void SetEmergencyFundThreshold(int clientId, DateTime date, decimal amount);

        List<PhysicalAsset> GetPhysicalAssets(int clientId);
        List<PhysicalAssetValueEntry> GetPhysicalAssetValueEntries(int clientId);

        PhysicalAsset AddPhysicalAsset(int clientId, string name);
        void RemovePhysicalAsset(string assetId);
        void SetPhysicalAssetValue(string assetId, DateTime date, decimal value, decimal debt, decimal interestRate);
    }
}
