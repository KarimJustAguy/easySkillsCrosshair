using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Licensing;

/// <summary>
/// The fixed catalog a Trial user can pick from: 4 shapes (Dot, Cross, Circle, T-Cross —
/// the T variant is a Cross layer with <see cref="CrosshairLayer.TStyle"/>), 4 colors, 4 sizes,
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

    public static readonly IReadOnlyList<double> Sizes = [2.0, 4.0, 6.0, 10.0];

    public const int MaxLayers = 1;
}
