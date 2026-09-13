using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Licensing.Steam;

namespace easySkillsCrosshair.Licensing;

/// <summary>
/// The single decision point for "Steam or Trial". Kept separate from constructing
/// <see cref="SteamworksAppsApi"/> (which touches native interop and can throw) so this
/// part — the actual fallback logic — is plain, deterministic and unit-testable.
/// </summary>
public static class LicenseProviderFactory
{
    public static ILicenseProvider CreateFromSteam(ISteamAppsApi? steamApps, uint proDlcAppId)
    {
        if (steamApps is null || !steamApps.IsInitialized)
        {
            // Steam client not running, not installed, or the native wrapper failed to
            // construct — Trial is always a valid, fully-functional state to start in.
            return new TrialLicenseProvider();
        }

        return new SteamLicenseProvider(steamApps, proDlcAppId);
    }
}
