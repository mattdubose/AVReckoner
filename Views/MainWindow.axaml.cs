using Avalonia.Controls;
using Avalonia.Input;
using AvReckoner.ViewModels;
using Reckoner.ViewModels;

namespace AvReckoner.Views
{
    public partial class MainWindow : Window
    {
        private Reckoner.Views.DebugLogWindow? _debugLogWindow;

        public MainWindow()
        {
            InitializeComponent();
            KeyDown += MainWindow_KeyDown;
        }

        // Hidden dev gesture — the raw fipy output log isn't meant for end users,
        // but it's still useful to have on hand while diagnosing sync issues.
        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.D || e.KeyModifiers != (KeyModifiers.Control | KeyModifiers.Shift))
                return;

            if (DataContext is not MainWindowViewModel { CurrentViewModel: MarketDataAdminViewModel marketVm })
                return;

            if (_debugLogWindow == null || !_debugLogWindow.IsVisible)
            {
                _debugLogWindow = new Reckoner.Views.DebugLogWindow { DataContext = marketVm };
                _debugLogWindow.Show();
            }
            else
            {
                _debugLogWindow.Activate();
            }
        }
    }
}