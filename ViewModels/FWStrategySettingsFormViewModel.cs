using PurpleValley.Extensions;
using Reckoner.Views;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Reckoner.ViewModels { 
public partial class FWStrategySettingsDialogViewModel : ObservableObject
{
    private readonly FWStrategySettingsWindow _window;

    [ObservableProperty]
    private FWStrategySettings strategySettings;

    public FWStrategySettingsDialogViewModel(FWStrategySettingsWindow window, FWStrategySettings current)
    {
        _window = window;

        // Make a copy so Cancel discards edits
        StrategySettings = current.Clone(); // implement Clone or copy ctor
    }

    [RelayCommand]
    private void Save() => _window.CloseWithResult(StrategySettings);

    [RelayCommand]
    private void Cancel() => _window.CloseWithResult(null);
}
}