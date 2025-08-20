using Microsoft.Extensions.DependencyInjection;
using PurpleValley.Utilities;
using Reckoner.Services;
using System;
using System.Linq;

namespace Reckoner.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        protected AppShellService _appShellService;
        protected BaseViewModel(AppShellService shellService) { _appShellService = shellService; }
        public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
    }

    public interface IViewModelFactory
    {
        T Create<T>() where T : BaseViewModel;
        public BaseViewModel CreateByName(string viewModelName);
    }

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _provider;

    public BaseViewModel CreateByName(string viewModelName)
    {
        // Find type by name
        var type = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t =>
                typeof(BaseViewModel).IsAssignableFrom(t) &&
                t.Name.Equals(viewModelName, StringComparison.OrdinalIgnoreCase));

        if (type is null)
            throw new InvalidOperationException($"ViewModel not found: {viewModelName}");

        return (BaseViewModel)_provider.GetRequiredService(type);
    }
    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Create<T>() where T : BaseViewModel
        => _provider.GetRequiredService<T>();
    }
}