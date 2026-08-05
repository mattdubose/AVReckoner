using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Reckoner.Views;

public partial class EpisodeDetailWindow : Window
{
    public EpisodeDetailWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
