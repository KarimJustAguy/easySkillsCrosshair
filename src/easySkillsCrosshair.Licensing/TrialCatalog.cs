using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Licensing;

/// <summary>
/// The fixed catalog a Trial user can pick from: 4 shapes (Dot, Cross, Circle, T-Cross —
/// the T variant is a Cross layer with <see cref="CrosshairLayer.TStyle"/>), 4 colors, one fixed size,
/// a single layer. Public so the UI can render exactly these options for Trial users.
/// </summary>
public static class TrialCatalog
{
    public static readonly IReadOnlyList<LayerType> LayerTypes =
    [
        LayerType.Dot,
        LayerType.Cross,
        LayerType.Circle,
    ];

    public static readonly IReadOnlyList<RgbaColor> Colors =
    [
        RgbaColor.FromHex("#D4AF37"), // easySkills gold
        RgbaColor.FromHex("#FFFFFF"),
        RgbaColor.FromHex("#00E676"),
        RgbaColor.FromHex("#FF1744"),
    ];

    /// <summary>Trial can't change the size; every layer uses this fixed default. Pro is free.</summary>
    public const double DefaultSize = 6.0;

    public const int MaxLayers = 1;
}
