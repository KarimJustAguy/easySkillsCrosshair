using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.App.ViewModels;

public static class LayerTypeNames
{
    public static string Get(LayerType type) => type switch
    {
        LayerType.Cross => "Fadenkreuz",
        LayerType.Dot => "Punkt",
        LayerType.Circle => "Kreis",
        LayerType.XCross => "X-Kreuz",
        LayerType.Box => "Box",
        LayerType.Chevron => "Chevron",
        LayerType.Line => "Linie",
        LayerType.Image => "Bild",
        _ => type.ToString(),
    };
}
