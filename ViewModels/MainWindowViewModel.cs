using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Windows.Input;

namespace AvReckoner.ViewModels
{
    public class MainWindowViewModel : PurpleValley.UIFramework.BaseViewModel
    {
        // This property holds the view model for the currently displayed view.
        private PurpleValley.UIFramework.BaseViewModel _currentViewModel;
        public PurpleValley.UIFramework.BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }
        public MainWindowViewModel(INavigationService navigation)
            :base (navigation)
        {
            AppShellService appShell = new AppShellService(navigation, null);
            CurrentViewModel = new WelcomePageViewModel(appShell);
       //     navigation.SetMainViewModel(this);
        }

    }
}
