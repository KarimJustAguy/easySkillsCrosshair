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

    private static CrosshairProfile ProOnlyProfile() => new()
    {
        Name = "pro",
        Layers =
        [
            new CrosshairLayer { Type = LayerType.XCross, Color = RgbaColor.FromHex("#123456"), Length = 42, OutlineThickness = 1 },
            new CrosshairLayer { Type = LayerType.Image, ImagePath = "reticle.png", Length = 16 },
        ],
        DynamicReactions = new DynamicReactionSettings { BloomOnFire = true },
    };

    [Fact]
    public void Trial_Rejects_Layer_Types_Outside_Catalog()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());

        Assert.True(gate.IsLayerTypeAllowed(LayerType.Dot));
        Assert.False(gate.IsLayerTypeAllowed(LayerType.XCross));
        Assert.False(gate.IsLayerTypeAllowed(LayerType.Image));
    }

    [Fact]
    public void Trial_Accepts_Default_Profile()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());
        var profile = CrosshairProfile.CreateDefault();
        profile.Layers[0].Length = TrialCatalog.Sizes[2];

        Assert.True(gate.IsProfileWithinLicense(profile));
    }

    [Fact]
    public void Trial_Rejects_Multiple_Layers()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());
        var profile = CrosshairProfile.CreateDefault();
        profile.Layers[0].Length = TrialCatalog.Sizes[0];
        profile.Layers.Add(new CrosshairLayer { Type = LayerType.Dot, Length = TrialCatalog.Sizes[0] });

        Assert.False(gate.IsProfileWithinLicense(profile));
    }

    [Fact]
    public void Pro_Allows_Everything()
    {
        var gate = new FeatureGate(new FakeLicenseProvider { CurrentTier = LicenseTier.Pro });

        Assert.True(gate.IsProfileWithinLicense(ProOnlyProfile()));
    }

    [Fact]
    public void Clamp_Reduces_Pro_Profile_To_Valid_Trial_Profile()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());

        var clamped = gate.ClampToLicense(ProOnlyProfile());

        Assert.True(gate.IsProfileWithinLicense(clamped));
        Assert.Single(clamped.Layers);
        Assert.False(clamped.DynamicReactions.IsActive);
    }

    [Fact]
    public void Clamp_Does_Not_Mutate_Input()
    {
        var gate = new FeatureGate(new FakeLicenseProvider());
        var original = ProOnlyProfile();

        gate.ClampToLicense(original);

        Assert.Equal(2, original.Layers.Count);
        Assert.True(original.DynamicReactions.BloomOnFire);
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
