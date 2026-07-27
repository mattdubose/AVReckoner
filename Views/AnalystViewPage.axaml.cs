using Avalonia.Controls;
using System.Linq;
using Reckoner.ViewModels;

namespace Reckoner.Views;

public partial class AnalystViewPage : UserControl
{
    public AnalystViewPage()
    {
        InitializeComponent();
    }

    private void ResultsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not AnalystViewModel vm) return;
        vm.UpdateSelectedResults(grid.SelectedItems.Cast<AnalystResultRow>().ToList());
    }
}
