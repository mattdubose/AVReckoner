using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvReckoner;
using Reckoner.ViewModels;

namespace Reckoner.Views;

public partial class InvestmentPerformancePage : UserControl
{
    public InvestmentPerformancePage()
    {
        InitializeComponent();
        Debug.WriteLine($"[InvestmentPerformancePage] DataContext = {DataContext?.GetType().Name ?? "null"}");
    }

    private async void EpisodesGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not DataGrid grid || DataContext is not InvestmentPerformanceViewModel vm) return;
        if (grid.SelectedItem is not EpisodeRow row) return;
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        await vm.ShowEpisodeDetail(row, owner);
    }
}
