using easySkillsCrosshair.Core.Licensing;

namespace easySkillsCrosshair.Licensing.Steam;

/// <summary>Pro is unlocked purely by owning the "easySkills Crosshair Pro" DLC on Steam.</summary>
public sealed class SteamLicenseProvider : ILicenseProvider
{
    private readonly ISteamAppsApi _steamApps;
    private readonly uint _proDlcAppId;
    private LicenseTier _currentTier = LicenseTier.Trial;

    public SteamLicenseProvider(ISteamAppsApi steamApps, uint proDlcAppId)
    {
        _steamApps = steamApps;
        _proDlcAppId = proDlcAppId;
    }

    public LicenseTier CurrentTier => _currentTier;

    public event EventHandler<LicenseTier>? TierChanged;

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var newTier = _steamApps.BIsDlcInstalled(_proDlcAppId) ? LicenseTier.Pro : LicenseTier.Trial;
        if (newTier != _currentTier)
        {
            _currentTier = newTier;
            TierChanged?.Invoke(this, _currentTier);
        }

        return Task.CompletedTask;
    }
}
