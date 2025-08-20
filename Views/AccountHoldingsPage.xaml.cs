namespace Reckoner.Views;

public partial class AccountHoldingsPage : ContentPage, INotifyPropertyChanged
{
    private readonly AccountHoldingsViewModel ViewModel;

    public AccountHoldingsPage(AccountHoldingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel = viewModel;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    public bool IsReadWrite
    {
        get => ViewModel.IsEditMode;
    }

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsEditMode))
        {
            OnPropertyChanged(nameof(IsReadWrite));
        }
    }

    public new event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		await ViewModel.LoadHoldingsAsync();
	}
    // ① Parameterless ctor for Shell:
    public AccountHoldingsPage()
    :  this(
        // ────────────────────────────────────────────────────────
        // Instead of MauiApp.Current.Services, use:
        Application.Current
                   .Handler
                   .MauiContext
                   .Services
                   .GetRequiredService<AccountHoldingsViewModel>()
      // ────────────────────────────────────────────────────────
      )*/
    {
    }
}
