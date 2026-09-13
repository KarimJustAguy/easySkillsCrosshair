namespace easySkillsCrosshair.Core.Licensing;

/// <summary>
/// Abstracts *how* the app learns its tier. Steam DLC entitlement is the first
/// implementation; a future license-server or offline-key model can implement this
/// same contract without touching anything that consumes <see cref="Licensing.IFeatureGate"/>.
/// </summary>
public interface ILicenseProvider
{
    LicenseTier CurrentTier { get; }
    event EventHandler<LicenseTier>? TierChanged;
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
