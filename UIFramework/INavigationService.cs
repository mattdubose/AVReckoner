using AvReckoner.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PurpleValley.UIFramework
{
    public interface INavigationService
    {
        public void SetMainViewModel(MainWindowViewModel main, IServiceProvider serviceProvider);

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel;
        public Task NavigateToAsync<TViewModel>() where TViewModel : BaseViewModel;
        bool CanGoBack { get; }
        ICommand GoBackCommand { get; }
        public void GoBack();
    }


}