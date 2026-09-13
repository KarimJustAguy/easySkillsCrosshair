using System.Windows;
using System.Windows.Media;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Reactions;
// UseWindowsForms adds a global `using System.Drawing;` — pin the WPF types explicitly.
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace easySkillsCrosshair.Overlay.Rendering;

/// <summary>
/// Draws a layered <see cref="CrosshairProfile"/> straight into a <see cref="DrawingContext"/>.
/// This is the single rendering path for the overlay window, the editor canvas and every
/// thumbnail. Immediate-mode drawing (instead of a tree of Shape elements rebuilt per change)
/// keeps high-frequency redraws — slider drags, bloom animation, GIF frames — allocation-light.
/// </summary>
public static class CrosshairDrawing
{
    public static void Draw(
        DrawingContext dc,
        CrosshairProfile profile,
        CrosshairRenderState state,
        Size surface,
        TimeSpan animationTime)
    {
        if (state.IsHidden)
        {
            return;
        }

        // Centre on a whole pixel so 1px/2px arms stay crisp instead of straddling two pixels.
        var centre = new Point(Math.Floor(surface.Width / 2), Math.Floor(surface.Height / 2));

        dc.PushTransform(new TranslateTransform(centre.X, centre.Y));
        dc.PushOpacity(Math.Clamp(profile.Opacity, 0, 1));
        dc.PushTransform(new RotateTransform(profile.RotationDegrees));

        foreach (var layer in profile.Layers)
        {
            if (layer.IsVisible)
            {
                DrawLayer(dc, layer, profile.DynamicReactions, state, animationTime);
            }
        }

        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private static void DrawLayer(
        DrawingContext dc,
        CrosshairLayer layer,
        DynamicReactionSettings reactions,
        CrosshairRenderState state,
        TimeSpan animationTime)
    {
        dc.PushTransform(new TranslateTransform(Math.Round(layer.OffsetX), Math.Round(layer.OffsetY)));
        dc.PushTransform(new RotateTransform(layer.RotationDegrees));
        dc.PushOpacity(Math.Clamp(layer.Opacity, 0, 1));

        var fill = CreateBrush(layer.Color);
        var outline = layer.OutlineThickness > 0 ? CreateBrush(layer.OutlineColor) : null;
        var bloomGap = reactions.BloomAmount * state.BloomFactor;

        switch (layer.Type)
        {
            case LayerType.Cross:
                DrawCross(dc, layer, fill, outline, layer.Gap + bloomGap, layer.TStyle || state.ForceTStyle);
                break;

            case LayerType.XCross:
                dc.PushTransform(new RotateTransform(45));
                DrawCross(dc, layer, fill, outline, layer.Gap + bloomGap, tStyle: false);
                dc.Pop();
                break;

            case LayerType.Dot:
                DrawDot(dc, layer, fill, outline);
                break;

            case LayerType.Circle:
                DrawRing(dc, layer, fill, outline, layer.Length + bloomGap);
                break;

            case LayerType.Box:
                DrawBox(dc, layer, fill, outline);
                break;

            case LayerType.Chevron:
                DrawChevron(dc, layer, fill, outline);
                break;

            case LayerType.Line:
                DrawBar(dc, new Rect(-layer.Length, -layer.Thickness / 2, layer.Length * 2, layer.Thickness), fill, outline, layer.OutlineThickness);
                break;

            case LayerType.Image:
                DrawImage(dc, layer, animationTime);
                break;
        }

        dc.Pop();
        dc.Pop();
        dc.Pop();
    }

    private static void DrawCross(DrawingContext dc, CrosshairLayer layer, Brush fill, Brush? outline, double gap, bool tStyle)
    {
        var t = Math.Max(0.5, layer.Thickness);
        var len = Math.Max(0, layer.Length);
        if (len <= 0) return;

        var half = t / 2;
        var o = layer.OutlineThickness;

        DrawBar(dc, new Rect(gap, -half, len, t), fill, outline, o);          // right
        DrawBar(dc, new Rect(-gap - len, -half, len, t), fill, outline, o);   // left
        DrawBar(dc, new Rect(-half, gap, t, len), fill, outline, o);          // bottom
        if (!tStyle)
        {
            DrawBar(dc, new Rect(-half, -gap - len, t, len), fill, outline, o); // top
        }
    }

    /// <summary>Outline is drawn as a slightly larger rect underneath, so it never eats into the fill.</summary>
    private static void DrawBar(DrawingContext dc, Rect rect, Brush fill, Brush? outline, double outlineThickness)
    {
        if (outline is not null)
        {
            var outer = rect;
            outer.Inflate(outlineThickness, outlineThickness);
            dc.DrawRectangle(outline, null, outer);
        }

        dc.DrawRectangle(fill, null, rect);
    }

    private static void DrawDot(DrawingContext dc, CrosshairLayer layer, Brush fill, Brush? outline)
    {
        var r = Math.Max(0.5, layer.Length / 2);
        if (outline is not null)
        {
            dc.DrawEllipse(outline, null, default, r + layer.OutlineThickness, r + layer.OutlineThickness);
        }

        dc.DrawEllipse(fill, null, default, r, r);
    }

    private static void DrawRing(DrawingContext dc, CrosshairLayer layer, Brush fill, Brush? outline, double radius)
    {
        var t = Math.Max(0.5, layer.Thickness);
        if (outline is not null)
        {
            dc.DrawEllipse(null, CreatePen(outline, t + layer.OutlineThickness * 2), default, radius, radius);
        }

        dc.DrawEllipse(null, CreatePen(fill, t), default, radius, radius);
    }

    private static void DrawBox(DrawingContext dc, CrosshairLayer layer, Brush fill, Brush? outline)
    {
        var edge = Math.Max(1, layer.Length);
        var rect = new Rect(-edge, -edge, edge * 2, edge * 2);
        var t = Math.Max(0.5, layer.Thickness);

        if (layer.Filled)
        {
            DrawBar(dc, rect, fill, outline, layer.OutlineThickness);
            return;
        }

        if (outline is not null)
        {
            dc.DrawRectangle(null, CreatePen(outline, t + layer.OutlineThickness * 2), rect);
        }

        dc.DrawRectangle(null, CreatePen(fill, t), rect);
    }

    private static void DrawChevron(DrawingContext dc, CrosshairLayer layer, Brush fill, Brush? outline)
    {
        var len = Math.Max(1, layer.Length);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(-len, len / 2), isFilled: false, isClosed: false);
            ctx.LineTo(new Point(0, -len / 2), isStroked: true, isSmoothJoin: true);
            ctx.LineTo(new Point(len, len / 2), isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();

        var t = Math.Max(0.5, layer.Thickness);
        if (outline is not null)
        {
            dc.DrawGeometry(null, CreatePen(outline, t + layer.OutlineThickness * 2), geometry);
        }

        dc.DrawGeometry(null, CreatePen(fill, t), geometry);
    }

    private static void DrawImage(DrawingContext dc, CrosshairLayer layer, TimeSpan animationTime)
    {
        var asset = ImageAssetCache.Get(layer.ImagePath);
        if (asset is null) return;

        var edge = Math.Max(1, layer.Length) * 2;
        var rect = new Rect(-edge / 2, -edge / 2, edge, edge);

        if (asset.Vector is { } vector)
        {
            var bounds = vector.Bounds;
            if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0) return;

            var scale = Math.Min(edge / bounds.Width, edge / bounds.Height);
            dc.PushTransform(new MatrixTransform(
                scale, 0, 0, scale,
                -bounds.X * scale - bounds.Width * scale / 2,
                -bounds.Y * scale - bounds.Height * scale / 2));
            dc.DrawDrawing(vector);
            dc.Pop();
            return;
        }

        if (asset.FrameAt(animationTime) is { } frame)
        {
            dc.DrawImage(frame, FitUniform(frame, rect));
        }
    }

    private static Rect FitUniform(ImageSource image, Rect bounds)
    {
        if (image.Width <= 0 || image.Height <= 0) return bounds;

        var scale = Math.Min(bounds.Width / image.Width, bounds.Height / image.Height);
        var w = image.Width * scale;
        var h = image.Height * scale;
        return new Rect(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }

    private static Brush CreateBrush(RgbaColor color)
    {
        var brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness) { StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat, LineJoin = PenLineJoin.Miter };
        pen.Freeze();
        return pen;
    }

    /// <summary>True if any visible layer needs continuous redraws (animated GIF).</summary>
    public static bool HasAnimatedContent(CrosshairProfile profile) =>
        profile.Layers.Any(l => l.IsVisible && l.Type == LayerType.Image && ImageAssetCache.Get(l.ImagePath)?.IsAnimated == true);
}
