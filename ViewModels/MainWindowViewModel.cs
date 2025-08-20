using PurpleValley.Utilities;
using Reckoner.ViewModels;

namespace Reckoner.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        public string Greeting { get; } = "Welcome to Avalonia!!!";
        
        public MainWindowViewModel(AppShellService appShell): base(appShell)
        {
              _appShellService.NavigateTo(nameof(WelcomePageViewModel)); // initial view
        }

        public BaseViewModel CurrentViewModel => _appShellService.Navigation.CurrentViewModel;
        
    }
}
