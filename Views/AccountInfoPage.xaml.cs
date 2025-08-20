namespace Reckoner.Views;

public partial class AccountInfoPage : ContentPage
{
  AccountInfoViewModel myViewModel;
	public AccountInfoPage(AccountInfoViewModel viewModel)
	{
		InitializeComponent();
        myViewModel = viewModel;
		BindingContext = viewModel;
	}
  // Handle the text changed event to update the filtered names
  void OnOwnerNameTextChanged(object sender, TextChangedEventArgs e)
  {
    myViewModel.UpdateFilteredNames(e.NewTextValue); // Update suggestions
  }

  // Handle the selection of an owner name
  void OnOwnerNameSelected(object sender, SelectionChangedEventArgs e)
  {
    if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
    {
      var selectedName = e.CurrentSelection.FirstOrDefault() as string;
      myViewModel.SetAccountOwner(selectedName); // Set the selected name
//      myViewModel.FilteredOwnerNames = new List<string>(); // Clear suggestions
    }
  }
}
