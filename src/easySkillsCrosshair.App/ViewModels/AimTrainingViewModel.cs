using easySkillsCrosshair.Core.Licensing;
using easySkillsCrosshair.Core.Mvvm;

namespace easySkillsCrosshair.App.ViewModels;

/// <summary>
/// Placeholder for the full Shooting Range module. Trial gets a preview note only;
/// the actual training scenes/metrics are a separate, larger feature not built in this pass.
/// </summary>
public sealed class AimTrainingViewModel : ViewModelBase
{
    private readonly IFeatureGate _featureGate;

    public AimTrainingViewModel(IFeatureGate featureGate)
    {
        _featureGate = featureGate;
        _featureGate.Changed += (_, _) => OnPropertyChanged(null);
    }

    // Computed, not captured in the constructor: the tier can change at runtime.
    public bool IsFullAccess => _featureGate.IsUnlocked(Feature.FullAimTrainer);

    public string AccessLevelText => IsFullAccess ? "Voller Zugriff (Pro)" : "Nur Vorschau (Trial)";
}
