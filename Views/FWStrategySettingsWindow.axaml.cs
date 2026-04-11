using Avalonia.Controls;

namespace Reckoner.Views;

public partial class FWStrategySettingsWindow : Window
{
    public FWStrategySettingsWindow()
    {
        InitializeComponent();
    }

    public void CloseWithResult(FWStrategySettings? result) => Close(result);
}
