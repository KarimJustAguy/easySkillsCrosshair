using System.Text.RegularExpressions;
using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.App.ViewModels;

public static partial class LayerTypeNames
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

    /// <summary>
    /// True for names the editor generated itself ("Punkt", "Punkt 2", …). Such names follow
    /// the layer's shape when it changes; anything the user typed is left alone.
    /// </summary>
    public static bool IsAutoName(string name)
    {
        var match = AutoNamePattern().Match(name.Trim());
        return match.Success && Enum.GetValues<LayerType>().Any(t => Get(t) == match.Groups["base"].Value);
    }

    [GeneratedRegex(@"^(?<base>.+?)(?: (?<n>\d+))?$")]
    private static partial Regex AutoNamePattern();
}
