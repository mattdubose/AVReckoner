
using LiveChartsCore.Kernel.Sketches;
using Microsoft.Maui.Animations;
using LiveChartsCore.SkiaSharpView.Maui;
namespace Reckoner.Views;

public partial class InvestmentPerformancePage : ContentPage
{
	InvestmentPerformanceViewModel _viewModel;
	public InvestmentPerformancePage(InvestmentPerformanceViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void PercentChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SimSettingsVM.UpdateComputed();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyChart.Handler?.DisconnectHandler();
        return;
        if (MyChart is CartesianChart chart)
        {
            chart.Series = null; 
        }
        _viewModel.ListOfLines.Clear();// = null;
        this.BindingContext = null;
    }
}
