using System.Windows.Media;
using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class ColorSwatchViewModel
{
    private readonly Lazy<System.Windows.Media.Brush> _brush;

    public ColorSwatchViewModel(RgbaColor color)
    {
        Color = color;
        _brush = new Lazy<System.Windows.Media.Brush>(() =>
        {
            var brush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
            brush.Freeze();
            return brush;
        });
    }

    public RgbaColor Color { get; }

    /// <summary>
    /// Created once and frozen. Previously this allocated a fresh brush on every getter call,
    /// which the editor's blanket property-changed refresh hit on every slider tick.
    /// </summary>
    public System.Windows.Media.Brush Brush => _brush.Value;
}
