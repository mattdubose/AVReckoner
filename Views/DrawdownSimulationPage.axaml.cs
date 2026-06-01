using Avalonia.Controls;
using System.Diagnostics;

namespace Reckoner.Views;

public partial class DrawdownSimulationPage : UserControl
{
    public DrawdownSimulationPage()
    {
        InitializeComponent();
        Debug.WriteLine($"[DrawdownSimulationPage] DataContext = {DataContext?.GetType().Name ?? "null"}");
    }
}
