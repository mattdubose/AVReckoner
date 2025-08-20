using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Repositories
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAccountsByClientIDAsync(int clientID);
        Task<List<Account>> GetAccountsByOwnerIDAsync(int ownerID);
        Task<List<Account>> GetAllAccountsAsync();
        Task<Account> GetAccountAsync(int accountID);
        Task<bool> CreateAccountAsync(Account account);
        Task<bool> UpdateAccountAsync(Account account);
    }

    public interface IAccountRepositorySync
  {
    List<Account> GetAccountsByClientID(Int32 clientID);
    List<Account> GetAccountsByOwnerID(Int32 ownerID);
    List<Account> GetAllAccounts();
    Account GetAccount(Int32 AccountID);

  }
}
