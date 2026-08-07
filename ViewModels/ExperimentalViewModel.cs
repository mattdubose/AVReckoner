namespace Reckoner.ViewModels
{
    // A holding pen for features that are visible enough to show a client where things are
    // headed, but not solid enough to sit alongside the finished pages — e.g. Drawdown
    // Simulation, whose RunSimulation() still just throws NotImplementedException. As each one
    // gets built out for real, move its nav entry back out of here.
    //
    // Hardcoded to William DuBose (ClientId 303) / Account 1 — these pages are reached directly
    // from the Welcome screen, skipping the normal Clients -> Account flow that would otherwise
    // set AppStateService.CurrentClient/CurrentAccount, so nothing would show up otherwise. Once
    // a feature graduates out of Experimental and gets its own real entry point via the Clients
    // flow, this hardcoding becomes irrelevant for it.
    public partial class ExperimentalViewModel : BaseViewModel
    {
        private const int DemoClientId = 303;
        private const int DemoAccountId = 1;

        private readonly AppStateService _appState;
        private readonly IClientRepository _clientRepo;
        private readonly IAccountRepository _accountRepo;

        public ExperimentalViewModel(AppShellService appShellService, AppStateService appState,
            IClientRepository clientRepo, IAccountRepository accountRepo) : base(appShellService)
        {
            _appState = appState;
            _clientRepo = clientRepo;
            _accountRepo = accountRepo;
        }

        private async Task SetDemoClientAndAccount()
        {
            _appState.CurrentClient = _clientRepo.GetClient(DemoClientId);
            try
            {
                _appState.CurrentAccount = await _accountRepo.GetAccountAsync(DemoAccountId);
            }
            catch (KeyNotFoundException)
            {
                Debug.WriteLine($"Experimental: demo account {DemoAccountId} not found.");
            }
        }

        [RelayCommand]
        private async Task OpenDrawdownSimulation()
        {
            await SetDemoClientAndAccount();
            await _appShellService.Navigation.NavigateToAsync<DrawdownSimulationViewModel>();
        }

        [RelayCommand]
        private async Task OpenNetWorth()
        {
            await SetDemoClientAndAccount();
            await _appShellService.Navigation.NavigateToAsync<NetWorthViewModel>();
        }

        [RelayCommand]
        private async Task OpenRothVsTraditional()
        {
            await SetDemoClientAndAccount();
            await _appShellService.Navigation.NavigateToAsync<RothVsTraditionalViewModel>();
        }
    }
}
