namespace easySkillsCrosshair.Licensing.Steam;

/// <summary>
/// Narrow seam around Steamworks.NET so <see cref="SteamLicenseProvider"/> and its tests
/// never touch the Steamworks API surface directly.
/// </summary>
public interface ISteamAppsApi
{
    bool IsInitialized { get; }
    bool BIsDlcInstalled(uint dlcAppId);

    /// <summary>
    /// Must be called before the process exits if <see cref="IsInitialized"/> is true —
    /// otherwise Steam can keep showing the game as "running" after the window closes.
    /// </summary>
    void Shutdown();
}
