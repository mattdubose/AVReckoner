using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
  public partial class NewClientViewModel : BaseViewModel
  {
    public ObservableCollection<Account> Accounts { get; set; }
    [ObservableProperty]
    bool readyForAccounts = false;
    [ObservableProperty]
    Client newClient;
    [ObservableProperty]
    Account newTempAccount; // placeholder that exists prior to putting it into the list.
    [ObservableProperty]
    bool spouseVisible;
    List<string> Names { get; set; }
    public NewClientViewModel(AppShellService appShell) : base(appShell) { newClient = new Client(); Accounts = new ObservableCollection<Account>(); newTempAccount = new Account();
      spouseVisible = false;
    }
    [RelayCommand]
    async Task Submit()
    {
      readyForAccounts = true;
    }


    public void SpouseBoxChange()
    {
      Debug.WriteLine("Get Here.");
      SpouseVisible = NewClient.IsMarried;
      if (NewClient.IsMarried == false)
      {
        NewClient.Spouse = null;
        return;
      }
      NewClient.Spouse = new Person();
      
    }
  }
}
