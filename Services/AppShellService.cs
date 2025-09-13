using Avalonia.Threading;
using Reckoner.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Services
{
    public class AppShellService
    {
        public INavigationService Navigation { get; }
        public IUiThreadDispatcher Dispatcher { get; }

        public AppShellService(INavigationService nav,  IUiThreadDispatcher dispatcher)
        {
            Navigation = nav;
            Dispatcher = dispatcher;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel 
        {
            Navigation.NavigateTo<TViewModel>();
        }
        public async Task NavigateToAsync<TViewModel>() where TViewModel : BaseViewModel 
        {
            await Navigation.NavigateToAsync<TViewModel>();
        }

    }
}
