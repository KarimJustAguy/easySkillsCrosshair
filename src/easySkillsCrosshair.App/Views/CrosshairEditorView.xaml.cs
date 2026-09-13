using System.Windows;
using easySkillsCrosshair.App.ViewModels;

namespace easySkillsCrosshair.App.Views;

/// <summary>
/// The editor mutates one profile in place, which a dependency property can't detect (same
/// reference). So the two live canvases are fed from the ViewModel's plain
/// <see cref="CrosshairEditorViewModel.PreviewChanged"/> event and redrawn explicitly.
///
/// Subscription is tied to Loaded/Unloaded: navigation recreates this view each time while the
/// ViewModel lives for the whole session, so a subscription that outlived the view would leak
/// every discarded instance.
/// </summary>
public partial class CrosshairEditorView : System.Windows.Controls.UserControl
{
    private CrosshairEditorViewModel? _subscribed;

    public CrosshairEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => Attach(DataContext as CrosshairEditorViewModel);
        Unloaded += (_, _) => Attach(null);
        DataContextChanged += (_, e) =>
        {
            if (IsLoaded) Attach(e.NewValue as CrosshairEditorViewModel);
        };
    }

    private void Attach(CrosshairEditorViewModel? vm)
    {
        if (ReferenceEquals(_subscribed, vm)) return;

        if (_subscribed is not null) _subscribed.PreviewChanged -= OnPreviewChanged;
        _subscribed = vm;
        if (vm is null) return;

        vm.PreviewChanged += OnPreviewChanged;
        Render(vm);
    }

    private void OnPreviewChanged(object? sender, EventArgs e)
    {
        if (_subscribed is { } vm) Render(vm);
    }

    private void Render(CrosshairEditorViewModel vm)
    {
        PreviewVisual.Profile = vm.Profile;
        PreviewVisual.Refresh();
        ActualSizeVisual.Profile = vm.Profile;
        ActualSizeVisual.Refresh();
    }
}
