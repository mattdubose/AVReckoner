namespace Reckoner.Views;

public partial class SecuritiesSelectionPage : ContentPage
{
  SecuritiesSelectionViewModel _viewModel;
  public SecuritiesSelectionPage(SecuritiesSelectionViewModel viewModel)
	{
		InitializeComponent();
    BindingContext = _viewModel = viewModel;
  }
}