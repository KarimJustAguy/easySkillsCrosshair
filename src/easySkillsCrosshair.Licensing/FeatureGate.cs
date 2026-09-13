using easySkillsCrosshair.Core.Crosshair;
using easySkillsCrosshair.Core.Licensing;

namespace easySkillsCrosshair.Licensing;

public sealed class FeatureGate : IFeatureGate
{
    private readonly ILicenseProvider _licenseProvider;

    public FeatureGate(ILicenseProvider licenseProvider)
    {
        _licenseProvider = licenseProvider;
        _licenseProvider.TierChanged += OnTierChanged;
    }

    public event EventHandler? Changed;

    public LicenseTier Tier => _licenseProvider.CurrentTier;

    private bool IsPro => Tier == LicenseTier.Pro;

    public bool IsLayerTypeAllowed(LayerType type) => IsPro || TrialCatalog.LayerTypes.Contains(type);

    public bool IsColorAllowed(RgbaColor color) => IsPro || TrialCatalog.Colors.Contains(color);

    public bool IsSizeAllowed(double size) => IsPro || TrialCatalog.Sizes.Contains(size);

    public bool IsUnlocked(Feature feature) => IsPro;

    public bool IsProfileWithinLicense(CrosshairProfile profile)
    {
        if (IsPro)
        {
            return true;
        }

        if (profile.Layers.Count > TrialCatalog.MaxLayers) return false;
        if (profile.DynamicReactions.IsActive) return false;

        return profile.Layers.All(layer =>
            IsLayerTypeAllowed(layer.Type)
            && IsColorAllowed(layer.Color)
            && IsSizeAllowed(layer.Length)
            && layer.OutlineThickness == 0
            && layer.ImagePath is null);
    }

    public CrosshairProfile ClampToLicense(CrosshairProfile profile)
    {
        var copy = profile.Clone();
        if (IsPro)
        {
            return copy;
        }

        var layer = copy.Layers.FirstOrDefault(l => l.Type != LayerType.Image)?.Clone()
                    ?? new CrosshairLayer { Name = "Fadenkreuz" };

        if (!IsLayerTypeAllowed(layer.Type)) layer.Type = LayerType.Cross;
        if (!IsColorAllowed(layer.Color)) layer.Color = TrialCatalog.Colors[0];
        if (!IsSizeAllowed(layer.Length)) layer.Length = NearestTrialSize(layer.Length);
        layer.OutlineThickness = 0;
        layer.ImagePath = null;
        layer.IsVisible = true;

        copy.Layers = [layer];
        copy.DynamicReactions = new DynamicReactionSettings();
        return copy;
    }

    private static double NearestTrialSize(double size) =>
        TrialCatalog.Sizes.OrderBy(s => Math.Abs(s - size)).First();

    private void OnTierChanged(object? sender, LicenseTier tier) => Changed?.Invoke(this, EventArgs.Empty);
}
