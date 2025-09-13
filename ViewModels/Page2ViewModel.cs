using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;

namespace AvReckoner.ViewModels
{
    public partial class Page2ViewModel : PurpleValley.UIFramework.BaseViewModel
    {
        public string HeaderText => "Welcome to Page two";
        public Page2ViewModel(INavigationService navigationService)
             : base(navigationService) { }

        public string ButtonText => "blah"; 
        [RelayCommand]
        private void GoToPage3()
        {
            NavService.NavigateTo<Page3ViewModel>();
        }

    }
}
