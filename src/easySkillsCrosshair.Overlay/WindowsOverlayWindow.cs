using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Core.Reactions;
using easySkillsCrosshair.Overlay.Input;
using easySkillsCrosshair.Overlay.Interop;
using easySkillsCrosshair.Overlay.Rendering;

namespace easySkillsCrosshair.Overlay;

/// <summary>
/// The passive overlay: a transparent, click-through, always-on-top window that renders
/// purely at the OS compositor level. No DirectX/OpenGL/Vulkan hooking, no code injection,
/// no reading the game's process memory — this window doesn't know a game exists.
///
/// Positioning uses raw Win32 physical pixels (via SetWindowPos) rather than WPF's
/// Left/Top DIP properties, which sidesteps the ambiguity of WPF's virtual-screen DIP
/// space across monitors that run different DPI scale factors. Visual content scaling
/// across monitors is left to WPF's own Per-Monitor-V2 support (declared in the host
/// app's app.manifest).
/// </summary>
public sealed class WindowsOverlayWindow : IOverlayHost
{
    /// <summary>Large enough for big layered crosshairs, custom images and full bloom.</summary>
    private const double CanvasSize = 256;
    private static readonly TimeSpan TopmostKeepAliveInterval = TimeSpan.FromSeconds(1);

    private readonly Window _window;
    private readonly CrosshairVisual _visual;
    private readonly DispatcherTimer _topmostKeepAliveTimer;
    private readonly ReactionEngine _reactions = new();

    private HwndSource? _hwndSource;
    private RawInputListener? _rawInput;
    private MonitorDescriptor? _targetMonitor;
    private CrosshairProfile? _profile;
    private int _offsetX;
    private int _offsetY;
    private bool _isRenderLoopActive;

    public OverlayHostKind Kind => OverlayHostKind.WindowsTopmostLayer;
    public bool IsVisible => _window.IsVisible;

    public WindowsOverlayWindow()
    {
        _visual = new CrosshairVisual { Width = CanvasSize, Height = CanvasSize, IsHitTestVisible = false };

        _window = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            Focusable = false,
            ShowActivated = false,
            Width = CanvasSize,
            Height = CanvasSize,
            Content = _visual,
        };
        _window.SourceInitialized += OnSourceInitialized;

        _topmostKeepAliveTimer = new DispatcherTimer { Interval = TopmostKeepAliveInterval };
        _topmostKeepAliveTimer.Tick += (_, _) => ReassertTopmost();

        _reactions.InputChanged += (_, _) => UpdateReactionState();
    }

    public void Initialize(OverlayHostOptions options)
    {
        _targetMonitor = options.InitialMonitor;
        _offsetX = options.InitialProfile.OffsetX;
        _offsetY = options.InitialProfile.OffsetY;
        UpdateContent(options.InitialProfile);
    }

    public void Show()
    {
        _window.Show();
        _topmostKeepAliveTimer.Start();
    }

    public void Hide()
    {
        _topmostKeepAliveTimer.Stop();
        _window.Hide();
    }

    public void SetOffset(int offsetX, int offsetY)
    {
        _offsetX = offsetX;
        _offsetY = offsetY;
        PositionWindow();
    }

    public void SetTargetMonitor(MonitorDescriptor monitor)
    {
        _targetMonitor = monitor;
        PositionWindow();
    }

    public void UpdateContent(CrosshairProfile profile)
    {
        // The editor mutates one profile in place; assigning the same reference wouldn't
        // trigger a redraw through the dependency property, so always refresh explicitly.
        _profile = profile;
        _visual.Profile = profile;
        _visual.Refresh();
        UpdateInputSubscription();
        UpdateReactionState();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = (HwndSource)PresentationSource.FromVisual(_window)!;
        ApplyClickThroughAndTopmostStyles(_hwndSource.Handle);
        PositionWindow();

        _rawInput = new RawInputListener(_hwndSource, _reactions);
        UpdateInputSubscription();
    }

    /// <summary>Raw input is only registered while the active profile actually uses reactions.</summary>
    private void UpdateInputSubscription()
    {
        if (_rawInput is null) return;

        if (_profile?.DynamicReactions.IsActive == true)
        {
            _rawInput.Start();
        }
        else
        {
            _rawInput.Stop();
        }
    }

    private void UpdateReactionState()
    {
        if (_profile is null) return;

        var now = DateTime.UtcNow;
        var state = _profile.DynamicReactions.IsActive
            ? _reactions.ComputeState(_profile.DynamicReactions, now)
            : CrosshairRenderState.Idle;

        if (_visual.State != state)
        {
            _visual.State = state;
        }

        // Keep a per-frame loop only while a bloom is decaying; idle overlay costs nothing.
        SetRenderLoop(_profile.DynamicReactions.IsActive && _reactions.IsAnimating(_profile.DynamicReactions, now));
    }

    private void SetRenderLoop(bool active)
    {
        if (active == _isRenderLoopActive) return;
        _isRenderLoopActive = active;

        if (active)
        {
            CompositionTarget.Rendering += OnRenderFrame;
        }
        else
        {
            CompositionTarget.Rendering -= OnRenderFrame;
        }
    }

    private void OnRenderFrame(object? sender, EventArgs e) => UpdateReactionState();

    private static void ApplyClickThroughAndTopmostStyles(IntPtr hwnd)
    {
        var exStyle = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
        exStyle |= NativeMethods.WS_EX_TRANSPARENT   // clicks fall through to the game/desktop
                 | NativeMethods.WS_EX_LAYERED       // required for per-pixel alpha + WS_EX_TRANSPARENT
                 | NativeMethods.WS_EX_TOPMOST
                 | NativeMethods.WS_EX_NOACTIVATE    // never steals focus/input from the game
                 | NativeMethods.WS_EX_TOOLWINDOW;   // hidden from Alt+Tab and the taskbar
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(exStyle));
    }

    private void PositionWindow()
    {
        if (_targetMonitor is null || _hwndSource is null)
        {
            return;
        }

        var bounds = _targetMonitor.PixelBounds;
        var centerXPx = bounds.X + bounds.Width / 2 + _offsetX;
        var centerYPx = bounds.Y + bounds.Height / 2 + _offsetY;

        var halfWidthPx = (int)(_window.Width / 2 * _targetMonitor.DpiScale);
        var halfHeightPx = (int)(_window.Height / 2 * _targetMonitor.DpiScale);

        NativeMethods.SetWindowPos(
            _hwndSource.Handle,
            IntPtr.Zero,
            centerXPx - halfWidthPx,
            centerYPx - halfHeightPx,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
    }

    /// <summary>
    /// Some games/other overlays reorder z-order on their own topmost transitions;
    /// re-asserting HWND_TOPMOST periodically is the standard mitigation.
    /// </summary>
    private void ReassertTopmost()
    {
        if (_hwndSource is null)
        {
            return;
        }

        NativeMethods.SetWindowPos(
            _hwndSource.Handle,
            NativeMethods.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    public void Dispose()
    {
        SetRenderLoop(false);
        _rawInput?.Dispose();
        _topmostKeepAliveTimer.Stop();
        _window.SourceInitialized -= OnSourceInitialized;
        _window.Close();
    }
}
