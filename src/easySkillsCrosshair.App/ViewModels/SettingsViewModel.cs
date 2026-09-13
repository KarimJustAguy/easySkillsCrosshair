using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IFeatureGate _featureGate;
    private readonly ILicenseSwitch? _licenseSwitch;

    /// <param name="licenseSwitch">
    /// Null in production wiring (real Steam entitlement). Only a dev/QA build supplies one,
    /// which is what makes the Trial/Pro toggle appear at all.
    /// </param>
    public SettingsViewModel(IFeatureGate featureGate, ILicenseSwitch? licenseSwitch = null)
    {
        _featureGate = featureGate;
        _licenseSwitch = licenseSwitch;
        _featureGate.Changed += (_, _) => OnPropertyChanged(null);
    }

    public LicenseTier Tier => _featureGate.Tier;
    public bool IsProUser => Tier == LicenseTier.Pro;
    public string TierDisplayName => IsProUser ? "Pro" : "Trial";

    /// <summary>Controls whether the switch is shown at all — hidden in a shipped build.</summary>
    public bool IsLicenseSwitchAvailable => _licenseSwitch is not null;

    /// <summary>
    /// Two-way bound to the Settings toggle. Flipping it raises TierChanged on the provider,
    /// which flows through IFeatureGate.Changed into every other ViewModel — no restart needed.
    /// </summary>
    public bool IsProEnabled
    {
        get => IsProUser;
        set
        {
            if (_licenseSwitch is null || IsProUser == value)
            {
                return;
            }

            _licenseSwitch.Tier = value ? LicenseTier.Pro : LicenseTier.Trial;
            // No OnPropertyChanged here: the Changed subscription above refreshes everything.
        }
    }
}
