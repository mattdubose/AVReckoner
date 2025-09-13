using Avalonia.Threading;
using AvReckoner.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PurpleValley.UIFramework
{
    public sealed partial class NavigationService : INavigationService
    {
        // A stack to keep a history of the view models for the back button.
        private readonly Stack<BaseViewModel> _history = new();

        private MainWindowViewModel _main;
        private IServiceProvider _serviceProvider;

        public ICommand GoBackCommand { get; }

        bool INavigationService.CanGoBack => _history.Count>0;

        public void SetMainViewModel (MainWindowViewModel main, IServiceProvider serviceProvider)
        {
            _main = main;
            _serviceProvider = serviceProvider;
        }
        public NavigationService()
        {
            GoBackCommand = new RelayCommand(GoBack, () => CanGoBack());
        }

        public void GoBack()
        {
            if (CanGoBack())
            {
                _main.CurrentViewModel = _history.Pop();
                ((RelayCommand)GoBackCommand).NotifyCanExecuteChanged();
            }
        }

        private bool CanGoBack() => _history.Count > 0;
        
        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
            _history.Push(_main.CurrentViewModel);
            if (viewModel!=null)
            _main.CurrentViewModel = (TViewModel)viewModel;
            ((RelayCommand)GoBackCommand).NotifyCanExecuteChanged();
        }
        public async Task NavigateToAsync<TViewModel>() where TViewModel : BaseViewModel
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // Call the InitializeAsync method on the new viewmodel.
            // This will run the override if it exists, otherwise it will do nothing.
            await viewModel.InitializeAsync();

            _history.Push(_main.CurrentViewModel);
            _main.CurrentViewModel = viewModel;
            ((RelayCommand)GoBackCommand).NotifyCanExecuteChanged();
        }
    }

    public sealed class AvaloniaUiDispatcher : IUiThreadDispatcher
    {
        public void Invoke(System.Action a) => Dispatcher.UIThread.Post(a); /* UIThread.Invoke(a) was here. */
        public Task InvokeAsync(Func<Task> f) => Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(f);
        public async Task ExecuteOnMainThreadAsync(System.Action action)
        {
            await Dispatcher.UIThread.InvokeAsync(action);
        }
    }

}
