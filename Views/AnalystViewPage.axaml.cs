using Avalonia.Controls;
using Avalonia.Input;
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

    private async void EpisodesGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not AnalystViewModel vm) return;
        if (grid.SelectedItem is not EpisodeRow row) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        await vm.ShowEpisodeDetail(row, owner);
    }
}
