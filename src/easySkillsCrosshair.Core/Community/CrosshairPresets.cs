using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Community;

/// <summary>Built-in starter crosshairs shown in the Community view, all built from layers.</summary>
public static class CrosshairPresets
{
    private static readonly RgbaColor Gold = RgbaColor.FromHex("#D4AF37");
    private static readonly RgbaColor White = RgbaColor.FromHex("#FFFFFF");
    private static readonly RgbaColor Green = RgbaColor.FromHex("#00E676");
    private static readonly RgbaColor Cyan = RgbaColor.FromHex("#18FFFF");
    private static readonly RgbaColor Red = RgbaColor.FromHex("#FF1744");
    private static readonly RgbaColor Black = RgbaColor.FromHex("#000000");

    public static IReadOnlyList<CommunityCrosshairListing> All { get; } =
    [
        Preset("classic-cross", "Classic Cross", "Klassisches Kreuz mit Lücke — der Allrounder.",
            new CrosshairLayer { Name = "Kreuz", Type = LayerType.Cross, Color = Green, Length = 6, Thickness = 2, Gap = 3 }),

        Preset("outlined-t", "Outlined T", "T-Form mit schwarzer Kontur, auf hellen Maps gut sichtbar.",
            new CrosshairLayer { Name = "T-Kreuz", Type = LayerType.Cross, Color = Cyan, Length = 7, Thickness = 2, Gap = 4, TStyle = true, OutlineThickness = 1, OutlineColor = Black }),

        Preset("dot-ring", "Dot & Ring", "Präzisionspunkt im Ring — ideal für Tracking.",
            new CrosshairLayer { Name = "Ring", Type = LayerType.Circle, Color = White, Length = 12, Thickness = 1.5 },
            new CrosshairLayer { Name = "Punkt", Type = LayerType.Dot, Color = Red, Length = 3 }),

        Preset("gold-precision", "Gold Precision", "easySkills-Signatur: Kreuz, Punkt und Kontur.",
            new CrosshairLayer { Name = "Kreuz", Type = LayerType.Cross, Color = Gold, Length = 5, Thickness = 2, Gap = 4, OutlineThickness = 1, OutlineColor = Black },
            new CrosshairLayer { Name = "Punkt", Type = LayerType.Dot, Color = Gold, Length = 2, OutlineThickness = 1, OutlineColor = Black }),

        Preset("x-marks", "X Marks", "Diagonales X — ungewohnt, aber sehr klar.",
            new CrosshairLayer { Name = "X", Type = LayerType.XCross, Color = White, Length = 6, Thickness = 2, Gap = 3, OutlineThickness = 1, OutlineColor = Black }),

        Preset("chevron-sight", "Chevron Sight", "Punkt im Chevron — Stil klassischer Visiere.",
            new CrosshairLayer { Name = "Chevron", Type = LayerType.Chevron, Color = Green, Length = 7, Thickness = 2, OffsetY = -2 },
            new CrosshairLayer { Name = "Punkt", Type = LayerType.Dot, Color = Green, Length = 2 }),

        Preset("box-frame", "Box Frame", "Offener Rahmen mit Mittelpunkt.",
            new CrosshairLayer { Name = "Rahmen", Type = LayerType.Box, Color = Cyan, Length = 9, Thickness = 1.5, Filled = false },
            new CrosshairLayer { Name = "Punkt", Type = LayerType.Dot, Color = Cyan, Length = 2 }),

        Preset("minimal-dot", "Minimal Dot", "Nur ein Punkt. Weniger ist mehr.",
            new CrosshairLayer { Name = "Punkt", Type = LayerType.Dot, Color = White, Length = 4, OutlineThickness = 1, OutlineColor = Black }),
    ];

    private static CommunityCrosshairListing Preset(string id, string name, string description, params CrosshairLayer[] layers) =>
        new($"preset:{id}", name, "easySkills", description, new CrosshairProfile { Name = name, Layers = [.. layers] }, IsImported: false);
}
