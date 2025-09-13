using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.ViewModels
{
    public partial class WelcomePageViewModel : BaseViewModel
    {
        public WelcomePageViewModel(AppShellService appShellService) :base(appShellService){ }

        [RelayCommand]
        private async Task NavigateToClients()
        {
            Console.Write("Navigating to Clients Page...\n");
            await _appShellService.Navigation.NavigateToAsync<ClientWelcomeViewModel>();
        }

        [RelayCommand]
        private async Task NavigateToSimulations()
        {
            await _appShellService.Navigation.NavigateToAsync<InvestmentPerformanceViewModel>();
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            throw new NotImplementedException();
//            await _appShellService.NavigateToAsync(nameof(Settins);
        }

    }
}
