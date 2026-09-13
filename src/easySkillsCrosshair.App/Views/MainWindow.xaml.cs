using System.ComponentModel;
using System.Windows;
using easySkillsCrosshair.App.ViewModels;

namespace easySkillsCrosshair.App.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
    }

    /// <summary>Called from App.OnExit right before Application.Shutdown() so this window
    /// can actually close instead of hiding itself again.</summary>
    public void AllowClose() => _allowClose = true;

    /// <summary>
    /// The overlay must keep running after this window closes, so "X" hides to the tray
    /// instead of shutting the app down — only the tray icon's "Beenden" really exits.
    /// </summary>
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
