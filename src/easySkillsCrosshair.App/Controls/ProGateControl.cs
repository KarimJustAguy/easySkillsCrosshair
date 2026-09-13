using System.Windows;
using System.Windows.Controls;

namespace easySkillsCrosshair.App.Controls;

/// <summary>
/// Wraps a control that's Pro-only: shown but dimmed/disabled with a small gold "PRO" badge
/// instead of being hidden outright, per the "visible but clearly marked" requirement.
/// Default look lives in Theme/Controls.xaml.
/// </summary>
public sealed class ProGateControl : ContentControl
{
    public static readonly DependencyProperty IsLockedProperty = DependencyProperty.Register(
        nameof(IsLocked), typeof(bool), typeof(ProGateControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(ProGateControl), new PropertyMetadata(false));

    static ProGateControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProGateControl), new FrameworkPropertyMetadata(typeof(ProGateControl)));
    }

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    /// <summary>Icon-only badge, for small targets (e.g. 56px shape tiles) where "PRO" won't fit.</summary>
    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }
}
