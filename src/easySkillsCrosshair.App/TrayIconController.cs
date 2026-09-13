using System.Drawing;
using System.Windows.Forms;

namespace easySkillsCrosshair.App;

/// <summary>
/// The app has no visible main window, so the tray icon is the only user-facing way to
/// exit cleanly. Deliberately has no dependency on System.Windows (WPF) — it only raises
/// <see cref="ExitRequested"/> and lets App.xaml.cs own the actual shutdown sequence.
/// </summary>
internal sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event EventHandler? ExitRequested;
    public event EventHandler? OpenRequested;

    public TrayIconController()
    {
        var openItem = new ToolStripMenuItem("Öffnen");
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);

        var exitItem = new ToolStripMenuItem("Beenden");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            // TODO: replace with the easySkills-branded icon once available.
            Icon = SystemIcons.Application,
            Text = "easySkills Crosshair",
            ContextMenuStrip = contextMenu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        // Visible = false first: a disposed-but-still-visible NotifyIcon can linger in the
        // tray until the user mouses over it, which reads as "the app didn't really quit".
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
