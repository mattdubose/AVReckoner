namespace Reckoner.Views;

public partial class ClientWelcomePage : ContentPage
{
	public ClientWelcomePage(ClientWelcomeViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
