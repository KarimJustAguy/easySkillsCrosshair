using System.Windows;
using System.Windows.Media;
using easySkillsCrosshair.Core.AimTraining;
// UseWindowsForms adds a global `using System.Drawing;` — pin the WPF types explicitly.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace easySkillsCrosshair.App.Controls;

/// <summary>Immediate-mode renderer for the shooting range: background grid + current targets.</summary>
public sealed class AimArenaElement : FrameworkElement
{
    private static readonly Brush Background = Frozen(new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x14)));
    private static readonly Pen GridPen = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22))), 1));
    private static readonly Brush TargetFill = Frozen(new SolidColorBrush(Color.FromRgb(0x1D, 0xB9, 0x7A)));
    private static readonly Pen TargetRim = Frozen(new Pen(Frozen(new SolidColorBrush(Color.FromRgb(0x0E, 0x6E, 0x48))), 2));
    private static readonly Brush TargetCore = Frozen(new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)));

    private const double GridSpacing = 40;

    public AimTrainingSession? Session { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        var bounds = new Rect(RenderSize);
        dc.DrawRectangle(Background, null, bounds);

        for (var x = GridSpacing; x < bounds.Width; x += GridSpacing)
        {
            dc.DrawLine(GridPen, new Point(x, 0), new Point(x, bounds.Height));
        }

        for (var y = GridSpacing; y < bounds.Height; y += GridSpacing)
        {
            dc.DrawLine(GridPen, new Point(0, y), new Point(bounds.Width, y));
        }

        if (Session is not { IsRunning: true } session) return;

        foreach (var target in session.Targets)
        {
            var centre = new Point(target.X, target.Y);
            dc.DrawEllipse(TargetFill, TargetRim, centre, target.Radius, target.Radius);
            dc.DrawEllipse(TargetCore, null, centre, Math.Max(2, target.Radius * 0.2), Math.Max(2, target.Radius * 0.2));
        }
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}
