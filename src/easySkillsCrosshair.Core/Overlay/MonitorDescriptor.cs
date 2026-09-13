namespace easySkillsCrosshair.Core.Overlay;

/// <summary>
/// DPI scale is captured per monitor (not per app), because Windows lets each
/// monitor in a multi-monitor setup run a different scale factor.
/// </summary>
public sealed record MonitorDescriptor(string DeviceName, PixelRect PixelBounds, double DpiScale, bool IsPrimary);
