using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using easySkillsCrosshair.App.ViewModels;
using Point = System.Windows.Point;

namespace easySkillsCrosshair.App.Views;

/// <summary>
/// Input + frame loop for the shooting range. Game rules live in the ViewModel/session; this
/// class only forwards pointer state, runs the per-frame tick while a session is active, and
/// positions the crosshair cursor. Everything is torn down on Unloaded so neither the frame loop
/// nor the ViewModel's events keep a discarded view alive.
/// </summary>
public partial class AimTrainingView : System.Windows.Controls.UserControl
{
    private AimTrainingViewModel? _vm;
    private bool _isPrimaryHeld;
    private Point _pointer;
    private bool _isLoopRunning;

    public AimTrainingView()
    {
        InitializeComponent();

        Loaded += (_, _) => Attach(DataContext as AimTrainingViewModel);
        Unloaded += (_, _) =>
        {
            _vm?.Stop();
            Attach(null);
        };
        DataContextChanged += (_, e) =>
        {
            if (IsLoaded) Attach(e.NewValue as AimTrainingViewModel);
        };

        ArenaHost.MouseMove += OnMouseMove;
        ArenaHost.MouseLeave += (_, _) => CursorVisual.Visibility = Visibility.Hidden;
        ArenaHost.MouseEnter += (_, _) => CursorVisual.Visibility = Visibility.Visible;
        ArenaHost.MouseLeftButtonDown += OnMouseDown;
        ArenaHost.MouseLeftButtonUp += OnMouseUp;
        ArenaHost.SizeChanged += (_, _) => _vm?.Resize(Arena.ActualWidth, Arena.ActualHeight);
    }

    private void Attach(AimTrainingViewModel? vm)
    {
        if (ReferenceEquals(_vm, vm)) return;

        if (_vm is not null)
        {
            _vm.SessionStartRequested -= OnSessionStartRequested;
            _vm.FrameUpdated -= OnFrameUpdated;
        }

        SetLoop(false);
        _vm = vm;
        if (vm is null) return;

        vm.SessionStartRequested += OnSessionStartRequested;
        vm.FrameUpdated += OnFrameUpdated;
        CursorVisual.Profile = vm.CursorProfile;
        CursorVisual.Refresh();
        Arena.Session = vm.Session;
        Arena.InvalidateVisual();
    }

    private void OnSessionStartRequested(object? sender, EventArgs e)
    {
        if (_vm is null) return;

        CursorVisual.Profile = _vm.CursorProfile;
        CursorVisual.Refresh();
        _vm.Start(Arena.ActualWidth, Arena.ActualHeight);
        Arena.Session = _vm.Session;
        SetLoop(true);
    }

    private void OnFrameUpdated(object? sender, EventArgs e)
    {
        Arena.InvalidateVisual();
        if (_vm is { IsRunning: false }) SetLoop(false);
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _pointer = e.GetPosition(Arena);
        Canvas.SetLeft(CursorVisual, _pointer.X - CursorVisual.Width / 2);
        Canvas.SetTop(CursorVisual, _pointer.Y - CursorVisual.Height / 2);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isPrimaryHeld = true;
        ArenaHost.CaptureMouse();
        _vm?.Shoot(_pointer.X, _pointer.Y);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPrimaryHeld = false;
        ArenaHost.ReleaseMouseCapture();
    }

    private void SetLoop(bool run)
    {
        if (run == _isLoopRunning) return;
        _isLoopRunning = run;

        if (run)
        {
            CompositionTarget.Rendering += OnRendering;
        }
        else
        {
            CompositionTarget.Rendering -= OnRendering;
        }
    }

    private void OnRendering(object? sender, EventArgs e) => _vm?.Tick(_isPrimaryHeld, _pointer.X, _pointer.Y);
}
