using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PurpleValley.Extensions;

namespace Reckoner.ViewModels
{
    public partial class FWStrategySettingsViewModel : BaseViewModel
    {
        public Action<object?>? ClosePopup { get; set; }
        
        [ObservableProperty]
        private string strategyName;
        public FWStrategySettings StrategySettings { get; }

        public FWStrategySettingsViewModel(AppShellService appShell, FWStrategySettings settings):base(appShell)
        {
            StrategySettings = settings.DeepCopy();
        }

        [RelayCommand]
        private void Save()
        {
            ClosePopup?.Invoke(StrategySettings);
        }

        [RelayCommand]
        private void Cancel()
        {
            ClosePopup?.Invoke(null);
        }
    }
}
