namespace Reckoner.Views;

public partial class NewClientForm : ContentPage
{
	NewClientViewModel myViewModel;
	public NewClientForm(NewClientViewModel viewModel)
	{
		InitializeComponent();
		myViewModel = viewModel;
		BindingContext = viewModel;
	}


  private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
  {
		myViewModel.SpouseBoxChange();
  }
}