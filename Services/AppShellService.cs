using Avalonia.Threading;
using PurpleValley.Utilities;
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
        public IViewModelFactory ViewModelFactory { get; }
        public IUiThreadDispatcher Dispatcher { get; }

        public AppShellService(INavigationService nav, IViewModelFactory factory, IUiThreadDispatcher dispatcher)
        {
            Navigation = nav;
            ViewModelFactory = factory;
            Dispatcher = dispatcher;
        }

        public void NavigateTo(string viewModelName)
        {
            var vm = ViewModelFactory.CreateByName(viewModelName);
            Navigation.NavigateTo(vm);
        }

        public async Task NavigateToAsync(string viewModelName)
        {
            var vm = ViewModelFactory.CreateByName(viewModelName);
            await Navigation.NavigateToAsync(vm);
        }
    }
}
