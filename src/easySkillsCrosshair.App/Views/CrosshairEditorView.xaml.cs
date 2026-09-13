using System.Windows.Controls;
using easySkillsCrosshair.App.ViewModels;
using easySkillsCrosshair.Overlay;

namespace easySkillsCrosshair.App.Views;

/// <summary>
/// The editor mutates one <see cref="Core.Crosshair.CrosshairProfile"/> instance in place (see
/// <see cref="CrosshairEditorViewModel"/>), so its live canvas can't rely on a WPF dependency
/// property's reference-equality change detection like <see cref="Controls.CrosshairPreviewControl"/>
/// does. It renders directly off the ViewModel's plain <see cref="CrosshairEditorViewModel.PreviewChanged"/>
/// event instead — still zero WPF knowledge inside the ViewModel itself.
/// </summary>
public partial class CrosshairEditorView : System.Windows.Controls.UserControl
{
    public CrosshairEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is CrosshairEditorViewModel oldVm)
        {
            oldVm.PreviewChanged -= OnPreviewChanged;
        }

        if (e.NewValue is CrosshairEditorViewModel newVm)
        {
            newVm.PreviewChanged += OnPreviewChanged;
            Render(newVm);
        }
    }

    private void OnPreviewChanged(object? sender, EventArgs e)
    {
        if (DataContext is CrosshairEditorViewModel vm)
        {
            Render(vm);
        }
    }

    private void Render(CrosshairEditorViewModel vm) =>
        SimpleCrosshairRenderer.Render(PreviewCanvas, vm.Profile);
}
