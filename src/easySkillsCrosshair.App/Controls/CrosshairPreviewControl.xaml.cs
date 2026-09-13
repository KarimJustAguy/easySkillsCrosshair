using System.Windows;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Overlay;

namespace easySkillsCrosshair.App.Controls;

/// <summary>
/// Read-only crosshair thumbnail for lists/cards/tiles (shape picker, Profiles, Community).
/// Renders through the same <see cref="SimpleCrosshairRenderer"/> as the real overlay and the
/// editor's live canvas. Each bound item is a distinct <see cref="CrosshairProfile"/> instance,
/// so the dependency property's default reference-equality change detection works correctly
/// here — unlike the editor, which mutates a single profile in place and re-renders via an
/// explicit event instead.
/// </summary>
public partial class CrosshairPreviewControl : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(
        nameof(Profile), typeof(CrosshairProfile), typeof(CrosshairPreviewControl),
        new PropertyMetadata(null, OnRenderInputChanged));

    public static readonly DependencyProperty CanvasSizeProperty = DependencyProperty.Register(
        nameof(CanvasSize), typeof(double), typeof(CrosshairPreviewControl),
        new PropertyMetadata(120.0, OnRenderInputChanged));

    public CrosshairPreviewControl()
    {
        InitializeComponent();
        ApplyCanvasSize();
    }

    public CrosshairProfile? Profile
    {
        get => (CrosshairProfile?)GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    /// <summary>Edge length of the render surface; the renderer centres on it.</summary>
    public double CanvasSize
    {
        get => (double)GetValue(CanvasSizeProperty);
        set => SetValue(CanvasSizeProperty, value);
    }

    private static void OnRenderInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CrosshairPreviewControl)d;
        control.ApplyCanvasSize();
        control.Render();
    }

    private void ApplyCanvasSize()
    {
        PreviewCanvas.Width = CanvasSize;
        PreviewCanvas.Height = CanvasSize;
    }

    private void Render()
    {
        if (Profile is { } profile)
        {
            SimpleCrosshairRenderer.Render(PreviewCanvas, profile);
        }
        else
        {
            PreviewCanvas.Children.Clear();
        }
    }
}
