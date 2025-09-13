using System.Windows.Input;

namespace AvReckoner.ViewModels
{
    // The other page ViewModels are very simple and just hold some text.
    public class Page1ViewModel : PurpleValley.UIFramework.BaseViewModel
    {

        public string HeaderText => "You are on Page One";
        public Page1ViewModel(INavigationService navigationService) 
            :base (navigationService) { }

    }
}
