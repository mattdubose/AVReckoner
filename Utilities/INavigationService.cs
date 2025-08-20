using Avalonia.Controls;
using Avalonia.Threading;
using Reckoner.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PurpleValley.Utilities
{
    public interface INavigationService
    {
        void NavigateTo(BaseViewModel viewModel);
        Task NavigateToAsync(BaseViewModel viewModel); 
        void GoBack();
        bool CanGoBack { get; }

        BaseViewModel CurrentViewModel { get; }
    }

    public class NavigationService : INotifyPropertyChanged, INavigationService
    {
        private readonly Stack<BaseViewModel> _history = new();
        private BaseViewModel _current;

        public BaseViewModel CurrentViewModel
        {
            get => _current;
            private set
            {
                if (_current != value)
                {
                    _current = value;
                    OnPropertyChanged(nameof(CurrentViewModel));
                    OnPropertyChanged(nameof(CanGoBack));
                }
            }
        }

        public bool CanGoBack => _history.Count > 1;

        public void NavigateTo(BaseViewModel viewModel)
        {
            if (_current != null)
            {
                _history.Push(_current);
            }
            CurrentViewModel = viewModel;
        }

        public async Task NavigateToAsync(BaseViewModel viewModel)
        {
            NavigateTo(viewModel); // set immediately
            await viewModel.OnNavigatedToAsync(); // let it initialize if needed
        }
        
        public void GoBack()
        {
            if (CanGoBack)
            {
                _history.Pop(); // discard current
                CurrentViewModel = _history.Peek(); // restore previous
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}