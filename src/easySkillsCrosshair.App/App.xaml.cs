using System.IO;
using System.Windows;
using easySkillsCrosshair.App.ViewModels;
using easySkillsCrosshair.App.Views;
using easySkillsCrosshair.Core.Community;
using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Overlay;
using easySkillsCrosshair.Core.Persistence;
using easySkillsCrosshair.Core.Sensitivity;
using easySkillsCrosshair.Licensing;
using easySkillsCrosshair.Licensing.Steam;
using easySkillsCrosshair.Overlay;

namespace easySkillsCrosshair.App;

public partial class App : System.Windows.Application
{
    // TODO: replace with the real Steam DLC AppId once "easySkills Crosshair Pro" is registered.
    private const uint ProDlcAppId = 0;

    private IOverlayHost? _overlay;
    private TrayIconController? _trayIcon;
    private ISteamAppsApi? _steamApps;
    private MainWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The tray icon is the only way to exit (no visible main window at first + ShutdownMode
        // OnExplicitShutdown), so it goes up first, before anything that could fail.
        _trayIcon = new TrayIconController();
        _trayIcon.ExitRequested += (_, _) =>
        {
            _mainWindow?.AllowClose();
            Shutdown();
        };
        _trayIcon.OpenRequested += (_, _) => ShowMainWindow();

        var (licenseProvider, licenseSwitch) = CreateLicensing();
        await licenseProvider.RefreshAsync();
        IFeatureGate featureGate = new FeatureGate(licenseProvider);

        var monitorProvider = new WindowsMonitorProvider();
        var initialProfile = featureGate.ClampToLicense(CrosshairProfile.CreateDefault());

        _overlay = OverlayHostFactory.Create(OverlayHostKind.WindowsTopmostLayer);
        _overlay.Initialize(new OverlayHostOptions(monitorProvider.GetPrimary(), initialProfile));
        _overlay.Show();

        // Composition root: every ViewModel below gets exactly the abstractions it needs
        // (IOverlayHost, IFeatureGate, IProfileStore, ICommunityService) — none of them
        // know about Steam, WPF windows, or the file system directly.
        var crosshairEditor = new CrosshairEditorViewModel(_overlay, featureGate);
        crosshairEditor.ApplyProfile(initialProfile);

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "easySkills", "Crosshair");

        IProfileStore profileStore = new JsonProfileStore();
        ICommunityService communityService = new LocalCommunityService(
            new CrosshairShareCodec(Path.Combine(dataDirectory, "media")),
            Path.Combine(dataDirectory, "community.json"));

        var mainViewModel = new MainViewModel(
            crosshairEditor,
            new AimTrainingViewModel(featureGate, crosshairEditor),
            CreateSensitivityConverter(dataDirectory),
            new ProfilesViewModel(profileStore, crosshairEditor),
            new CommunityViewModel(communityService, crosshairEditor, featureGate),
            new SettingsViewModel(featureGate, licenseSwitch));

        _mainWindow = new MainWindow(mainViewModel);
        _mainWindow.Show();
    }

    /// <summary>
    /// Debug builds (running from the IDE): always a runtime-switchable provider starting in Pro,
    /// so both tiers can be tested from Settings without any setup.
    /// Release builds: the real Steam DLC check — unless explicitly opted into dev licensing via
    /// EASYSKILLS_DEV_LICENSE=1 (starts Trial) or EASYSKILLS_FORCE_PRO=1 (starts Pro). A shipped
    /// Steam build therefore never contains a reachable free Pro switch.
    /// </summary>
    private (ILicenseProvider Provider, ILicenseSwitch? Switch) CreateLicensing()
    {
#if DEBUG
        var isDebugBuild = true; // not const: avoids unreachable-code warnings for the other branch
#else
        var isDebugBuild = false;
#endif
        var forcePro = Environment.GetEnvironmentVariable("EASYSKILLS_FORCE_PRO") == "1";
        var devLicense = Environment.GetEnvironmentVariable("EASYSKILLS_DEV_LICENSE") == "1";

        if (isDebugBuild || forcePro || devLicense)
        {
            var startTier = isDebugBuild || forcePro ? LicenseTier.Pro : LicenseTier.Trial;
            var switchable = new SwitchableLicenseProvider(startTier);
            return (switchable, switchable);
        }

        _steamApps = TryCreateSteamAppsApi();
        return (LicenseProviderFactory.CreateFromSteam(_steamApps, ProDlcAppId), null);
    }

    /// <summary>Prefers the editable Data\games.json next to the exe; falls back to the embedded copy.</summary>
    private static SensitivityConverterViewModel CreateSensitivityConverter(string dataDirectory)
    {
        var catalog = GameCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Data", "games.json"));
        return new SensitivityConverterViewModel(
            catalog.Games,
            catalog.Warning,
            Path.Combine(dataDirectory, "sensitivity-converter.json"));
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    /// <summary>
    /// Constructing the Steamworks.NET wrapper is the only part of startup that can fail
    /// for reasons outside our control (missing/mismatched steam_api64.dll, no Steam client
    /// running in the background, etc.). Any failure here must fall back to Trial silently —
    /// never crash the app. The actual Steam-vs-Trial decision lives in
    /// <see cref="LicenseProviderFactory"/>, which stays testable because it never touches
    /// native interop itself.
    /// </summary>
    private static ISteamAppsApi? TryCreateSteamAppsApi()
    {
        try
        {
            return new SteamworksAppsApi();
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _overlay?.Dispose();
        _steamApps?.Shutdown();
        base.OnExit(e);

        // Belt-and-braces: if anything (a stray timer, the WinForms message loop backing
        // the tray icon, ...) kept a foreground thread alive, Steam would keep reporting
        // this game as "running" forever. Guarantee full process termination.
        Environment.Exit(0);
    }
}
