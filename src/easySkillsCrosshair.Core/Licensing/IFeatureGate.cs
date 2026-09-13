using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Licensing;

/// <summary>
/// The single place the rest of the app asks "am I allowed to do this". Nothing
/// outside the Licensing project needs to know whether the tier came from Steam,
/// a future license server, or a dev switch.
/// </summary>
public interface IFeatureGate
{
    /// <summary>
    /// Raised when the underlying tier changed, so ViewModels can re-evaluate every
    /// lock/unlock state instead of caching it once at construction time.
    /// </summary>
    event EventHandler? Changed;

    LicenseTier Tier { get; }

    bool IsLayerTypeAllowed(LayerType type);
    bool IsColorAllowed(RgbaColor color);
    bool IsSizeAllowed(double size);
    bool IsUnlocked(Feature feature);

    /// <summary>Convenience check combining all of the above for a whole profile.</summary>
    bool IsProfileWithinLicense(CrosshairProfile profile);

    /// <summary>Returns a copy of <paramref name="profile"/> reduced to what the current tier permits.</summary>
    CrosshairProfile ClampToLicense(CrosshairProfile profile);
}
