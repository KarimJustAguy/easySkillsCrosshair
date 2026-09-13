namespace easySkillsCrosshair.Core.Crosshair;

public sealed class CrosshairProfile
{
    public required string Name { get; set; }

    /// <summary>Rendered bottom → top.</summary>
    public List<CrosshairLayer> Layers { get; set; } = [];

    /// <summary>Whole-crosshair transforms, applied on top of each layer's own.</summary>
    public double RotationDegrees { get; set; }
    public double Opacity { get; set; } = 1.0;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public DynamicReactionSettings DynamicReactions { get; set; } = new();

    public static CrosshairProfile CreateDefault(string name = "Default") => new()
    {
        Name = name,
        Layers = [new CrosshairLayer { Name = "Fadenkreuz", Type = LayerType.Cross }],
    };

    public CrosshairProfile Clone() => new()
    {
        Name = Name,
        Layers = Layers.Select(l => l.Clone()).ToList(),
        RotationDegrees = RotationDegrees,
        Opacity = Opacity,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        DynamicReactions = DynamicReactions.Clone(),
    };
}
