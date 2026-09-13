namespace easySkillsCrosshair.Core.Overlay;

/// <summary>A monitor's bounds in physical pixels (not WPF's DPI-scaled DIPs).</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height);
