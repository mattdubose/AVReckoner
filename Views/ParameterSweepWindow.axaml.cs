using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using Reckoner.ViewModels;

namespace Reckoner.Views;

public partial class ParameterSweepWindow : Window
{
    public ParameterSweepWindow()
    {
        InitializeComponent();
    }

    private void ResultsGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not ParameterSweepViewModel vm) return;
        vm.UpdateSelectedResults(grid.SelectedItems.Cast<AnalystResultRow>().ToList());
    }

    private async void EpisodesGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not ParameterSweepViewModel vm) return;
        if (grid.SelectedItem is not EpisodeRow row) return;
        await vm.ShowEpisodeDetail(row, this);
    }
}
