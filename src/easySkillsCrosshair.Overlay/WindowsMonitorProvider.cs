using System.Windows.Forms;
using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Overlay.Interop;

namespace easySkillsCrosshair.Overlay;

/// <summary>Reads real monitor bounds and per-monitor DPI from Win32/WinForms, once, on demand.</summary>
public sealed class WindowsMonitorProvider : IMonitorProvider
{
    public MonitorDescriptor GetPrimary() => ToDescriptor(Screen.PrimaryScreen!);

    public IReadOnlyList<MonitorDescriptor> GetAll() =>
        Screen.AllScreens.Select(ToDescriptor).ToArray();

    private static MonitorDescriptor ToDescriptor(Screen screen)
    {
        var bounds = screen.Bounds; // physical pixels
        var center = new NativeMethods.POINT
        {
            X = bounds.X + bounds.Width / 2,
            Y = bounds.Y + bounds.Height / 2,
        };

        var hMonitor = NativeMethods.MonitorFromPoint(center, NativeMethods.MONITOR_DEFAULTTONEAREST);
        NativeMethods.GetDpiForMonitor(hMonitor, NativeMethods.MDT_EFFECTIVE_DPI, out var dpiX, out _);

        return new MonitorDescriptor(
            DeviceName: screen.DeviceName,
            PixelBounds: new PixelRect(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            DpiScale: dpiX / 96.0,
            IsPrimary: screen.Primary);
    }
}
