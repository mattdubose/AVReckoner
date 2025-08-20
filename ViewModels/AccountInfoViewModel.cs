using Reckoner.Repositories;
using System.Collections.Generic;

namespace Reckoner.ViewModels;
public partial class AccountInfoViewModel : BaseViewModel
{

  public ObservableCollection<Account> Accounts { get; set; }
  bool canAddAccount;
  Client? OwningClient = null;
  IAccountRepository _repository;
  [ObservableProperty]
  List<string> filteredOwnerNames = new List<string>();

  Account newAccount = null;

  public AccountInfoViewModel(AppShellService appShell, Client owningClient, IAccountRepository respoitory) : base(appShell) 
  {
    _repository = respoitory;
    if (owningClient != null)
    {
      OwningClient = owningClient;
//      Accounts = new ObservableCollection<Account>(_repository.GetAccountsByClientIDAsync(owningClient.ClientId));
    }
  }
  [RelayCommand]
  async Task AddAccount()
  {
//    Account.Equals
  }
  public void UpdateFilteredNames(string input)
  {
    /* 
     * if (string.IsNullOrWhiteSpace(input))
    {
      FilteredOwnerNames = new List<string>(); // Clear suggestions if no input
    }
    else
    {
      foreach (var owner in PossibleOwners)
      {
        if (owner.FirstName.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
        {
          FilteredOwnerNames.Add(owner.FirstName);
        }
      }
    }*/
  }
  public void SetAccountOwner(string? selectedName)
  {
    //      NewTempAccount.Owner.FirstName = selectedName;
  }

}
