using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PurpleValley.UIFramework;

// This base class comes from the CommunityToolkit.Mvvm package
// and provides the boilerplate for property change notifications.
public partial class BaseViewModel : ObservableObject
{
    // Make the property public or internal so it can be accessed by derived classes
    protected INavigationService NavService { get; }

    public BaseViewModel(INavigationService navigationService)
    {
        // This is where you set the property from the injected value.
        NavService = navigationService;
        GoBackCommand = NavService.GoBackCommand;
        CanGoBack = NavService.CanGoBack;
    }
    protected AppShellService _appShellService;
    protected BaseViewModel(AppShellService shellService) { _appShellService = shellService; }
    bool CanGoBack { get; }    

    public ICommand GoBackCommand { get; }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}