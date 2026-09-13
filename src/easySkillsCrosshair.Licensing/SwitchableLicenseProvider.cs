using easySkillsCrosshair.Core.Licensing;

namespace easySkillsCrosshair.Licensing;

/// <summary>
/// Dev/QA-only provider whose tier can be flipped at runtime (Settings → Trial/Pro toggle),
/// so Pro-only UI can be exercised before a real Steam DLC entitlement exists. Only ever wired
/// in behind an explicit opt-in (see App.xaml.cs's EASYSKILLS_DEV_LICENSE check) — never the
/// default path, so real Trial/Pro behaviour is never silently bypassed in a shipped build.
/// </summary>
public sealed class SwitchableLicenseProvider : ILicenseProvider, ILicenseSwitch
{
    private LicenseTier _currentTier;

    public SwitchableLicenseProvider(LicenseTier initialTier = LicenseTier.Trial)
    {
        _currentTier = initialTier;
    }

    public event EventHandler<LicenseTier>? TierChanged;

    public LicenseTier CurrentTier => _currentTier;

    public LicenseTier Tier
    {
        get => _currentTier;
        set
        {
            if (_currentTier == value)
            {
                return;
            }

            _currentTier = value;
            TierChanged?.Invoke(this, value);
        }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
