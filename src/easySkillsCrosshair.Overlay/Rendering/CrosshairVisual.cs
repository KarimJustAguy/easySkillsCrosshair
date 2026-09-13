using System.Windows;
using System.Windows.Media;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Reactions;

namespace easySkillsCrosshair.Overlay.Rendering;

/// <summary>
/// Lightweight element that renders a crosshair via <see cref="CrosshairDrawing"/> in OnRender.
/// Used by the overlay window, the editor's live canvas and all thumbnails. Only subscribes to
/// per-frame rendering while it actually shows an animated GIF, and only while loaded.
/// </summary>
public sealed class CrosshairVisual : FrameworkElement
{
    public static readonly DependencyProperty ProfileProperty = DependencyProperty.Register(
        nameof(Profile), typeof(CrosshairProfile), typeof(CrosshairVisual),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnProfileChanged));

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
        nameof(State), typeof(CrosshairRenderState), typeof(CrosshairVisual),
        new FrameworkPropertyMetadata(CrosshairRenderState.Idle, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private bool _isAnimating;

    public CrosshairVisual()
    {
        ClipToBounds = true;
        Loaded += (_, _) => UpdateAnimationSubscription();
        Unloaded += (_, _) => SetAnimating(false);
    }

    public CrosshairProfile? Profile
    {
        get => (CrosshairProfile?)GetValue(ProfileProperty);
        set => SetValue(ProfileProperty, value);
    }

    public CrosshairRenderState State
    {
        get => (CrosshairRenderState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>
    /// Call after mutating the bound profile in place: the dependency property can't detect
    /// that (same reference), so the owner must request the redraw explicitly.
    /// </summary>
    public void Refresh()
    {
        InvalidateVisual();
        UpdateAnimationSubscription();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        // Transparent background makes the whole surface part of the visual bounds.
        drawingContext.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(RenderSize));

        if (Profile is { } profile)
        {
            CrosshairDrawing.Draw(drawingContext, profile, State, RenderSize, DateTime.UtcNow - _startedUtc);
        }
    }

    private static void OnProfileChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((CrosshairVisual)d).UpdateAnimationSubscription();

    private void UpdateAnimationSubscription() =>
        SetAnimating(IsLoaded && Profile is { } p && CrosshairDrawing.HasAnimatedContent(p));

    private void SetAnimating(bool animate)
    {
        if (animate == _isAnimating) return;
        _isAnimating = animate;

        if (animate)
        {
            CompositionTarget.Rendering += OnFrame;
        }
        else
        {
            CompositionTarget.Rendering -= OnFrame;
        }
    }

    private void OnFrame(object? sender, EventArgs e) => InvalidateVisual();
}
