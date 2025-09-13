using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection; // This is the key using statement
using AvReckoner.ViewModels;
using AvReckoner.Views;
using System;

namespace AvReckoner
{
    public static class AppPaths
    {
        public const string AppName = "Reckoner";

        // Where the app is installed / running from (for read-only packaged files)
        public static string InstalledDir => AppContext.BaseDirectory;

        // Per-user data directory (safe to write)
        public static string UserDataDir
        {
            get
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var path = Path.Combine(root, AppName);
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public static string InstalledFile(string file) => Path.Combine(InstalledDir, file);
        public static string UserFile(string file) => Path.Combine(UserDataDir, file);
    }
    public partial class App : Application
    {
        // 1. A static property to hold the DI container.
        public static IServiceProvider Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 2. Configure the ServiceCollection and register all types.
            var services = new ServiceCollection();

            services.AddSingleton<IClientRepository>(sp =>
               new ClientJSONRepository(AppPaths.InstalledFile("Data/Clients.json")));

            // Writable accounts in user data dir
            services.AddSingleton<IAccountRepository>(sp =>
            {
                var accountsPath = AppPaths.UserFile("Accounts.json");
                if (!File.Exists(accountsPath))
                {
                    var seed = AppPaths.InstalledFile("Data/Accounts.json");
                    if (File.Exists(seed)) File.Copy(seed, accountsPath);
                    else File.WriteAllText(accountsPath, "[]"); // empty list as fallback
                }
                return new AccountJsonRepository(accountsPath);
            });

            // SQLite example
            services.AddSingleton(sp =>
            {
                var dbPath = AppPaths.UserFile("Reckoner.db");
                if (!File.Exists(dbPath))
                {
                    var seed = AppPaths.InstalledFile("Data/Reckoner.db");
                    if (File.Exists(seed)) File.Copy(seed, dbPath);
                }
                var connString = $"Data Source={dbPath}";
                return connString; // or your IDbConnection/DbContext wrapper
            });
            // Register services and viewmodels
            // AddSingleton is for services you want one instance of.
            services.AddSingleton<IUiThreadDispatcher, AvaloniaUiDispatcher>();
            services.AddSingleton<INavigationService, NavigationService>();
            // Register AppStateService as a singleton:
            services.AddSingleton<AppStateService>();

            services.AddSingleton<AppShellService>();
            services.AddSingleton<MainWindowViewModel>();

            // AddTransient is for viewmodels you want a new instance of each time.
            services.AddTransient<WelcomeViewModel>();
            services.AddTransient<WelcomePageViewModel>();
            services.AddTransient<Page1ViewModel>();
            services.AddTransient<Page2ViewModel>();
            services.AddTransient<Page3ViewModel>();
            services.AddTransient<AccountHoldingsViewModel>();
            services.AddTransient<ClientWelcomeViewModel>();

            // 3. Build the IServiceProvider from the ServiceCollection.
            Services = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var navigationService = Services.GetRequiredService<INavigationService>();
                var mainWindowViewModel = Services.GetRequiredService<MainWindowViewModel>();

                // Set the MainWindowViewModel on the NavigationService
                ((NavigationService)navigationService).SetMainViewModel(mainWindowViewModel, Services);

                desktop.MainWindow = new MainWindow
                {
                    // 4. Get the MainWindowViewModel from the DI container to set the DataContext.
                    DataContext = Services.GetRequiredService<MainWindowViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}