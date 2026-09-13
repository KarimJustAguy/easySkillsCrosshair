namespace easySkillsCrosshair.Core.Crosshair;

/// <summary>
/// One composable element of a crosshair. A profile is an ordered stack of these
/// (bottom → top), which is what makes the shape system effectively unlimited.
/// Not every parameter applies to every <see cref="LayerType"/>; unused ones are ignored.
/// </summary>
public sealed class CrosshairLayer
{
    public string Name { get; set; } = "Ebene";
    public LayerType Type { get; set; } = LayerType.Cross;
    public bool IsVisible { get; set; } = true;

    public RgbaColor Color { get; set; } = RgbaColor.FromHex("#D4AF37");

    /// <summary>Arm length (Cross/XCross/Line/Chevron), radius (Circle), diameter (Dot), edge (Box), size (Image).</summary>
    public double Length { get; set; } = 6;
    public double Thickness { get; set; } = 2;
    /// <summary>Distance from centre to where arms start (Cross/XCross).</summary>
    public double Gap { get; set; } = 3;
    public bool TStyle { get; set; }
    public bool Filled { get; set; } = true;

    public double OutlineThickness { get; set; }
    public RgbaColor OutlineColor { get; set; } = new(0, 0, 0);

    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double RotationDegrees { get; set; }

    public string? ImagePath { get; set; }

    public CrosshairLayer Clone() => (CrosshairLayer)MemberwiseClone();
}
