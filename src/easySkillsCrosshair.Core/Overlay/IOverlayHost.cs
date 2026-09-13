using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Overlay;

/// <summary>
/// Everything the app knows about "where the crosshair is drawn". The only implementation
/// today is <c>WindowsOverlayWindow</c> (a passive topmost layered window); a future
/// <c>GameBarOverlayHost</c> plugs in behind the same contract for exclusive-fullscreen games.
/// </summary>
public interface IOverlayHost : IDisposable
{
    OverlayHostKind Kind { get; }
    bool IsVisible { get; }

    void Initialize(OverlayHostOptions options);
    void Show();
    void Hide();
    void SetOffset(int offsetX, int offsetY);
    void SetTargetMonitor(MonitorDescriptor monitor);
    void UpdateContent(CrosshairProfile profile);
}
