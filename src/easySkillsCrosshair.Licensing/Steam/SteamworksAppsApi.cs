using Steamworks;

namespace easySkillsCrosshair.Licensing.Steam;

/// <summary>
/// Real Steamworks.NET-backed implementation. Requires steam_api64.dll next to the
/// executable and a running Steam client (or a steam_appid.txt during development).
/// </summary>
public sealed class SteamworksAppsApi : ISteamAppsApi
{
    public bool IsInitialized { get; }

    public SteamworksAppsApi()
    {
        IsInitialized = SteamAPI.Init();
    }

    public bool BIsDlcInstalled(uint dlcAppId) =>
        IsInitialized && SteamApps.BIsDlcInstalled(new AppId_t(dlcAppId));

    public void Shutdown()
    {
        if (IsInitialized)
        {
            SteamAPI.Shutdown();
        }
    }
}
