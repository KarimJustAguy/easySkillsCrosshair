using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Licensing.Steam;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public class LicenseProviderFactoryTests
{
    private sealed class FakeSteamAppsApi : ISteamAppsApi
    {
        public bool IsInitialized { get; init; }
        public bool DlcInstalled { get; init; }
        public bool ShutdownCalled { get; private set; }

        public bool BIsDlcInstalled(uint dlcAppId) => DlcInstalled;
        public void Shutdown() => ShutdownCalled = true;
    }

    [Fact]
    public void Falls_Back_To_Trial_When_Steam_Client_Is_Not_Running()
    {
        // SteamworksAppsApi.IsInitialized is false exactly when SteamAPI.Init() failed,
        // e.g. because no Steam client is running in the background.
        var steamApps = new FakeSteamAppsApi { IsInitialized = false };

        var provider = LicenseProviderFactory.CreateFromSteam(steamApps, proDlcAppId: 1);

        Assert.IsType<TrialLicenseProvider>(provider);
        Assert.Equal(LicenseTier.Trial, provider.CurrentTier);
    }

    [Fact]
    public void Falls_Back_To_Trial_When_Steam_Wrapper_Could_Not_Be_Constructed_At_All()
    {
        // Mirrors App.xaml.cs's TryCreateSteamAppsApi() returning null after catching
        // a native-interop failure (e.g. steam_api64.dll missing).
        var provider = LicenseProviderFactory.CreateFromSteam(null, proDlcAppId: 1);

        Assert.IsType<TrialLicenseProvider>(provider);
        Assert.Equal(LicenseTier.Trial, provider.CurrentTier);
    }

    [Fact]
    public async Task Uses_Steam_Entitlement_When_Steam_Is_Initialized()
    {
        var steamApps = new FakeSteamAppsApi { IsInitialized = true, DlcInstalled = true };

        var provider = LicenseProviderFactory.CreateFromSteam(steamApps, proDlcAppId: 1);
        await provider.RefreshAsync();

        Assert.IsType<SteamLicenseProvider>(provider);
        Assert.Equal(LicenseTier.Pro, provider.CurrentTier);
    }
}
