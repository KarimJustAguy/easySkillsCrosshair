using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Overlay.GameBar;

namespace easySkillsCrosshair.Overlay;

public static class OverlayHostFactory
{
    public static IOverlayHost Create(OverlayHostKind kind) => kind switch
    {
        OverlayHostKind.WindowsTopmostLayer => new WindowsOverlayWindow(),
        OverlayHostKind.GameBarWidget => new GameBarOverlayHost(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
