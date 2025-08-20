using Reckoner.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Reckoner.Utilities;

namespace Reckoner.Repositories;
public class IAssetJsonConverter : JsonConverter<IAsset>
{
  public override IAsset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
    {
      JsonElement root = doc.RootElement;
      AssetType assetType = Enum.Parse<AssetType>(root.GetProperty("AssetType").GetString());
      string id = root.GetProperty("ID").GetString();

      return assetType switch
      {
        AssetType.MarketSecurity => new MarketSecurity { TickerSymbol = id },
        AssetType.RealEstate => new RentalProperty { Address = id },
        _ => throw new NotSupportedException($"Asset type '{assetType}' is not supported")
      };
    }
  }

  public override void Write(Utf8JsonWriter writer, IAsset value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();
    writer.WriteString("AssetType", value.AssetType.ToString());
    writer.WriteString("ID", value.ID);
    writer.WriteEndObject();
  }
}

public class AccountJsonRepository : IAccountRepository
{
  private readonly string _filePath;
  private List<Account> _accounts;

  public AccountJsonRepository(string filePath)
  {
    _filePath = filePath;
    LoadAccounts();
  }

  private void LoadAccounts()
  {
    Stream stream= null;
    try
    {
      stream = FileUtils.OpenFile(_filePath);
    }
    catch 
    {
      Debug.WriteLine($"Coudn't open file {_filePath} ");
      _accounts = new List<Account>();
      return;
    }

/*    var options = new JsonSerializerOptions
    {
      Converters = { new IAssetJsonConverter() }, // Custom converter for IAsset
      PropertyNameCaseInsensitive = true
    };*/
    _accounts = JsonSerializer.Deserialize<List<Account>>(stream) ?? new List<Account>();
  }

  public Account GetAccount(int accountId)
  {
    return _accounts.FirstOrDefault(a => a.AccountId == accountId);
  }

  public List<Account> GetAccountsByClientID(int clientID)
  {
    return _accounts.Where(a => a.ClientId == clientID).ToList();
  }

  public List<Account> GetAccountsByOwnerID(int ownerID)
  {
    return _accounts.Where(a => a.OwnerId == ownerID).ToList();
  }

  public List<Account> GetAllAccounts()
  {
    return _accounts;
  }
    public Task<List<Account>> GetAccountsByClientIDAsync(int clientID)
    {
        if (_accounts == null)
    
            return Task.FromResult(new List<Account>());

        var matches = _accounts.Where(a => a.ClientId == clientID).ToList();
        return Task.FromResult(matches);
    }

    public Task<List<Account>> GetAccountsByOwnerIDAsync(int ownerID)
    {
        if (_accounts == null)
            return Task.FromResult(new List<Account>());

        var matches = _accounts.Where(a => a.AccountId == ownerID).ToList();
        return Task.FromResult(matches);
    }

    public Task<List<Account>> GetAllAccountsAsync()
    {
        return Task.FromResult(_accounts ?? new List<Account>());
    }

    public Task<Account> GetAccountAsync(int accountID)
    {
        if (_accounts == null)
            throw new KeyNotFoundException();

        var acct = _accounts.FirstOrDefault(a => a.AccountId == accountID)
                  ?? throw new KeyNotFoundException();

        return Task.FromResult(acct);
    }

    public Task<bool> CreateAccountAsync(Account account)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateAccountAsync(Account account)
    {
        throw new NotImplementedException();
    }
}
