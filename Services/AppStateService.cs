using System.ComponentModel;
using System.Runtime.CompilerServices;
using Reckoner.Models;

namespace Reckoner.Services
{
    public class AppStateService : INotifyPropertyChanged
    {
        Client? _currentClient;
        public Client? CurrentClient
        {
            get => _currentClient;
            set
            {
                if (_currentClient?.ClientId == value?.ClientId) return;
                _currentClient = value;
                OnPropertyChanged();
            }
        }

        Account? _currentAccount;
        public Account? CurrentAccount
        {
            get => _currentAccount;
            set
            {
                if (_currentAccount?.AccountId == value?.AccountId) return;
                _currentAccount = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
