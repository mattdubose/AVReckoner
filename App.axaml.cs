using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Reckoner.ViewModels;
using AvReckoner.Views;
using Microsoft.Extensions.DependencyInjection;
using PurpleValley.Utilities;
using Reckoner.ViewModels;
using System;
using System.Linq;
namespace Reckoner
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();

            // Register your services here
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IUiThreadDispatcher, AvaloniaMainThreadDispatcher>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<AppShellService, AppShellService>();
            services.AddSingleton<WelcomePageViewModel, WelcomePageViewModel>();

            Services = services.BuildServiceProvider();


            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}