using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvReckoner;

namespace Reckoner.Views;

public partial class InvestmentPerformancePage : UserControl
{
    public InvestmentPerformancePage()
    {
        InitializeComponent();
        Debug.WriteLine($"[InvestmentPerformancePage] DataContext = {DataContext?.GetType().Name ?? "null"}");
    }
}
