using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Reckoner.Models;
using Reckoner.Repositories;
using Reckoner.Services;

namespace Reckoner.ViewModels
{
    public partial class ClientWelcomeViewModel : BaseViewModel
    {
        readonly IClientRepository _clientRepo;
        readonly IAccountRepository _accountRepo;
        readonly AppStateService _appState;

        // All clients to choose from:
        public ObservableCollection<Client> Clients { get; } = new();

        // All accounts for the selected client:
        public ObservableCollection<Account> Accounts { get; } = new();

        [ObservableProperty] Client? selectedClient;
        [ObservableProperty] Account? selectedAccount;

        public ICommand ContinueCommand { get; }
        public ICommand CreateNewClientCommand { get; }

        public ClientWelcomeViewModel(
            IClientRepository clientRepo,
            IAccountRepository accountRepo,
            AppStateService appState,
            AppShellService appShell) : base(appShell)
        {
            _clientRepo = clientRepo;
            _accountRepo = accountRepo;
            _appState = appState;

            ContinueCommand = new RelayCommand(Continue, CanContinue);
            CreateNewClientCommand = new RelayCommand(OpenNewClientForm);

            LoadClients();
        }

        void LoadClients()
        {
            Clients.Clear();
            var all = _clientRepo.GetAllClients();
            foreach (var c in all)
                Clients.Add(c);
        }

        partial void OnSelectedClientChanged(Client? oldClient, Client? newClient)
        {
            // Whenever the client changes, reload that client’s accounts:
            Accounts.Clear();
            SelectedAccount = null;

            if (newClient != null)
            {
                // Sync or async—using .Result here for brevity. In production, do it async.
                var accounts = _accountRepo.GetAccountsByClientIDAsync(SelectedClient.ClientId).Result;
                foreach (var acct in accounts)
                    Accounts.Add(acct);
            }

            // Notify that ContinueEnabled (and button CanExecute) might have changed:
            ((RelayCommand)ContinueCommand).NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(AccountPickerVisible));
        }

        partial void OnSelectedAccountChanged(Account? oldAcct, Account? newAcct)
        {
            ((RelayCommand)ContinueCommand).NotifyCanExecuteChanged();
        }

        // Show account picker only once a client is picked:
        public bool AccountPickerVisible => SelectedClient != null;

        bool CanContinue() => SelectedClient != null && SelectedAccount != null;

        void Continue()
        {  
            if (SelectedClient == null || SelectedAccount == null)
                return;

            // Save both choices in AppState:
            _appState.CurrentClient = SelectedClient;
            _appState.CurrentAccount = SelectedAccount;

         }
        [RelayCommand]
        public async Task NewContinueAsync()
        {
            if (SelectedClient == null || SelectedAccount == null)
                return;

            _appState.CurrentClient = SelectedClient;
            _appState.CurrentAccount = SelectedAccount;

            await _appShellService.NavigateToAsync(nameof(AccountHoldingsViewModel));
        }
        void OpenNewClientForm()
        {
            // Navigate to NewClientForm (if you have it) to create a new client:
        }
    }
}
