using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;
using Xunit;

namespace easySkillsCrosshair.Licensing.Tests;

public class FeatureGateTests
{
    private sealed class FakeLicenseProvider : ILicenseProvider
    {
        private LicenseTier _currentTier = LicenseTier.Trial;

        public LicenseTier CurrentTier
        {
            get => _currentTier;
            set
            {
                _currentTier = value;
                TierChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<LicenseTier>? TierChanged;

        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public void Trial_Rejects_Shape_Outside_Fixed_Catalog()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());

        Assert.False(gate.IsShapeAllowed(CrosshairShape.Custom));
        Assert.True(gate.IsShapeAllowed(CrosshairShape.Dot));
    }

    [Fact]
    public void Trial_Rejects_Profile_With_Custom_Media_Upload()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());
        var profile = new CrosshairProfile { Name = "x", CustomMediaPath = "reticle.png" };

        Assert.False(gate.IsProfileWithinLicense(profile));
    }

    [Fact]
    public void Trial_Rejects_Profile_With_Dynamic_Reactions()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());
        var profile = new CrosshairProfile
        {
            Name = "x",
            DynamicReactions = new DynamicReactionSettings { BloomOnFire = true },
        };

        Assert.False(gate.IsProfileWithinLicense(profile));
    }

    [Fact]
    public void Pro_Allows_Everything()
    {
        var license = new FakeLicenseProvider { CurrentTier = LicenseTier.Pro };
        var gate = new FeatureGate(license);
        var profile = new CrosshairProfile
        {
            Name = "x",
            Shape = CrosshairShape.Custom,
            CustomMediaPath = "reticle.svg",
            Color = RgbaColor.FromHex("#123456"),
            Size = 42,
            DynamicReactions = new DynamicReactionSettings { BloomOnFire = true },
        };

        Assert.True(gate.IsProfileWithinLicense(profile));
    }

    [Fact]
    public void Tier_Change_Is_Reflected_Immediately()
    {
        var license = new FakeLicenseProvider();
        var gate = new FeatureGate(license);

        Assert.False(gate.IsUnlocked(Feature.DynamicReactions));

        license.CurrentTier = LicenseTier.Pro;

        Assert.True(gate.IsUnlocked(Feature.DynamicReactions));
    }

    [Fact]
    public void Tier_Change_Raises_Changed_So_ViewModels_Can_Refresh()
    {
        var license = new FakeLicenseProvider();
        var gate = new FeatureGate(license);
        var raised = 0;
        gate.Changed += (_, _) => raised++;

        license.CurrentTier = LicenseTier.Pro;

        Assert.Equal(1, raised);
    }
}
