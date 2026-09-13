namespace easySkillsCrosshair.Core.Licensing;

/// <summary>
/// Dev/QA seam: lets a test build flip the tier at runtime. Production wiring (real Steam
/// entitlement) does NOT provide this — consumers must treat it as optional and hide any
/// switching UI when it is absent.
/// </summary>
public interface ILicenseSwitch
{
    LicenseTier Tier { get; set; }
}
