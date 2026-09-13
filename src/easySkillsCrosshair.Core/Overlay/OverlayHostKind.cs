namespace easySkillsCrosshair.Core.Overlay;

public enum OverlayHostKind
{
    /// <summary>Passive, click-through, always-on-top desktop window (see architecture notes).</summary>
    WindowsTopmostLayer,

    /// <summary>
    /// Reserved extension point for a future Xbox Game Bar widget, which is the only
    /// sanctioned way to render over a game running in true exclusive fullscreen.
    /// Not implemented yet; see GameBarOverlayHost.
    /// </summary>
    GameBarWidget,
}
