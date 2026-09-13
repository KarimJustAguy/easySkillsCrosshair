using easySkillsCrosshair.Core.Licensing;

namespace easySkillsCrosshair.Licensing;

/// <summary>
/// Default/fallback provider: always Trial. Used when Steam isn't reachable
/// (dev machine, SteamAPI.Init() failed) so the app degrades instead of crashing.
/// </summary>
public sealed class TrialLicenseProvider : ILicenseProvider
{
    public LicenseTier CurrentTier => LicenseTier.Trial;

#pragma warning disable CS0067 // never raised: tier is fixed for this provider
    public event EventHandler<LicenseTier>? TierChanged;
#pragma warning restore CS0067

    public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
