using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Reckoner.Models;
using Reckoner.Utilities;

namespace Reckoner.Repositories
{
    // Same shape as AccountJsonRepository — a single writable JSON file, loaded fully into memory
    // and rewritten whole on every save. Net worth data is small (a handful of accounts and one
    // balance entry per account per check-in), so this stays simple until it doesn't.
    public class NetWorthJsonRepository : INetWorthRepository
    {
        private class NetWorthData
        {
            public List<NetWorthAccount> Accounts { get; set; } = new();
            public List<NetWorthBalanceEntry> Balances { get; set; } = new();
            public List<EmergencyFundThresholdEntry> Thresholds { get; set; } = new();
            public List<PhysicalAsset> PhysicalAssets { get; set; } = new();
            public List<PhysicalAssetValueEntry> PhysicalAssetValues { get; set; } = new();
        }

        private readonly string _filePath;
        private NetWorthData _data;

        public NetWorthJsonRepository(string filePath)
        {
            _filePath = filePath;
            Load();
        }

        private void Load()
        {
            try
            {
                using var stream = FileUtils.OpenFile(_filePath);
                _data = JsonSerializer.Deserialize<NetWorthData>(stream) ?? new NetWorthData();
            }
            catch
            {
                Debug.WriteLine($"Couldn't open file {_filePath}");
                _data = new NetWorthData();
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save net worth data to {_filePath}: {ex.Message}");
            }
        }

        public List<NetWorthAccount> GetAccounts(int clientId) =>
            _data.Accounts.Where(a => a.ClientId == clientId).ToList();

        public List<NetWorthBalanceEntry> GetBalanceEntries(int clientId)
        {
            var accountIds = GetAccounts(clientId).Select(a => a.Id).ToHashSet();
            return _data.Balances.Where(b => accountIds.Contains(b.AccountId)).ToList();
        }

        public List<EmergencyFundThresholdEntry> GetEmergencyFundThresholds(int clientId) =>
            _data.Thresholds.Where(t => t.ClientId == clientId).ToList();

        public NetWorthAccount AddAccount(int clientId, string name, NetWorthCategory category)
        {
            var account = new NetWorthAccount { ClientId = clientId, Name = name, Category = category };
            _data.Accounts.Add(account);
            Save();
            return account;
        }

        public void RemoveAccount(string accountId)
        {
            _data.Accounts.RemoveAll(a => a.Id == accountId);
            _data.Balances.RemoveAll(b => b.AccountId == accountId);
            Save();
        }

        public void SetBalance(string accountId, DateTime date, decimal balance)
        {
            var existing = _data.Balances.FirstOrDefault(b => b.AccountId == accountId && b.Date.Date == date.Date);
            if (existing != null)
                existing.Balance = balance;
            else
                _data.Balances.Add(new NetWorthBalanceEntry { AccountId = accountId, Date = date.Date, Balance = balance });
            Save();
        }

        public void SetEmergencyFundThreshold(int clientId, DateTime date, decimal amount)
        {
            var existing = _data.Thresholds.FirstOrDefault(t => t.ClientId == clientId && t.Date.Date == date.Date);
            if (existing != null)
                existing.Amount = amount;
            else
                _data.Thresholds.Add(new EmergencyFundThresholdEntry { ClientId = clientId, Date = date.Date, Amount = amount });
            Save();
        }

        public List<PhysicalAsset> GetPhysicalAssets(int clientId) =>
            _data.PhysicalAssets.Where(a => a.ClientId == clientId).ToList();

        public List<PhysicalAssetValueEntry> GetPhysicalAssetValueEntries(int clientId)
        {
            var assetIds = GetPhysicalAssets(clientId).Select(a => a.Id).ToHashSet();
            return _data.PhysicalAssetValues.Where(v => assetIds.Contains(v.AssetId)).ToList();
        }

        public PhysicalAsset AddPhysicalAsset(int clientId, string name)
        {
            var asset = new PhysicalAsset { ClientId = clientId, Name = name };
            _data.PhysicalAssets.Add(asset);
            Save();
            return asset;
        }

        public void RemovePhysicalAsset(string assetId)
        {
            _data.PhysicalAssets.RemoveAll(a => a.Id == assetId);
            _data.PhysicalAssetValues.RemoveAll(v => v.AssetId == assetId);
            Save();
        }

        public void SetPhysicalAssetValue(string assetId, DateTime date, decimal value, decimal debt, decimal interestRate)
        {
            var existing = _data.PhysicalAssetValues.FirstOrDefault(v => v.AssetId == assetId && v.Date.Date == date.Date);
            if (existing != null)
            {
                existing.Value = value;
                existing.Debt = debt;
                existing.InterestRate = interestRate;
            }
            else
            {
                _data.PhysicalAssetValues.Add(new PhysicalAssetValueEntry
                {
                    AssetId = assetId,
                    Date = date.Date,
                    Value = value,
                    Debt = debt,
                    InterestRate = interestRate,
                });
            }
            Save();
        }
    }
}
