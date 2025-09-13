namespace AvReckoner.ViewModels
{
    public class Page3ViewModel : PurpleValley.UIFramework.BaseViewModel
    {
        public string HeaderText => "Welcome to Page 3";
        public Page3ViewModel(INavigationService navigationService)
             : base(navigationService) { }
    }
}
