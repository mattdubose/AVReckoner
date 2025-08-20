namespace Reckoner.Views;

public partial class FWStrategySettingsPopup : CommunityToolkit.Maui.Views.Popup
{
    public FWStrategySettingsPopup(FWStrategySettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.ClosePopup = Close;
    }
    private void OnApplyClicked(object sender, EventArgs e)
    {
        var vm = (FWStrategySettingsViewModel)BindingContext;
        Close(vm.StrategySettings); // returns result to caller
    }
    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(); // returns result to caller
    }
}