using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace AvReckoner.ViewModels
{
    public class WelcomeViewModel : PurpleValley.UIFramework.BaseViewModel
    {
        public string Greeting => "Welcome! Please choose a page.";

        // Commands that will be bound to the buttons on the WelcomeView
        public ICommand NavigateToPage1Command { get; }
        public ICommand NavigateToPage2Command { get; }
        public ICommand NavigateToPage3Command { get; }

            // The constructor for this class must call the base constructor
            public WelcomeViewModel(INavigationService navigationService)
                : base(navigationService)
            {
            // Your other viewmodel-specific initialization here
            NavigateToPage1Command = new RelayCommand(() => NavService.NavigateTo<Page1ViewModel>());
            NavigateToPage2Command = new RelayCommand(() => NavService.NavigateTo<Page2ViewModel>());
            NavigateToPage3Command = new RelayCommand(() => NavService.NavigateTo<Page3ViewModel>());
        }
        
    }
}
