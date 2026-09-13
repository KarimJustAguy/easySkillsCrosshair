using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Overlay;

namespace easySkillsCrosshair.Overlay.GameBar;

/// <summary>
/// Extension point for a future Xbox Game Bar widget project. A Game Bar widget runs in
/// a compositor surface Windows already grants access to over true exclusive-fullscreen
/// games, which <see cref="WindowsOverlayWindow"/> (a normal desktop window) cannot reach.
///
/// Not implemented in this phase, by design. Once the separate widget project exists,
/// wiring it in is a one-line change to <see cref="OverlayHostFactory"/> — nothing that
/// consumes <see cref="IOverlayHost"/> needs to change.
/// </summary>
public sealed class GameBarOverlayHost : IOverlayHost
{
    public OverlayHostKind Kind => OverlayHostKind.GameBarWidget;
    public bool IsVisible => false;

    public void Initialize(OverlayHostOptions options) => throw NotImplemented();
    public void Show() => throw NotImplemented();
    public void Hide() => throw NotImplemented();
    public void SetOffset(int offsetX, int offsetY) => throw NotImplemented();
    public void SetTargetMonitor(MonitorDescriptor monitor) => throw NotImplemented();
    public void UpdateContent(CrosshairProfile profile) => throw NotImplemented();
    public void Dispose() { }

    private static NotSupportedException NotImplemented() =>
        new("Game Bar widget host is a planned extension point and ships in a later phase.");
}
