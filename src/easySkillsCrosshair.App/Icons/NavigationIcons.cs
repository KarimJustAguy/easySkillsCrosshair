using System.Windows.Media;

namespace easySkillsCrosshair.App.Icons;

public enum IconKind
{
    Crosshair,
    Target,
    Calculator,
    Layers,
    Globe,
    Settings,
}

/// <summary>
/// Small, original, straight-line-only vector glyphs for the sidebar (no arcs, so
/// <see cref="Geometry.Parse"/> can't fail on malformed arc flags). Placeholder-quality —
/// swap for branded icons later without touching any binding that consumes <see cref="Get"/>.
/// </summary>
public static class NavigationIcons
{
    public static Geometry Get(IconKind kind)
    {
        var geometry = Geometry.Parse(kind switch
        {
            IconKind.Crosshair => "M12,2 L12,22 M2,12 L22,12",
            IconKind.Target => "M12,2 L22,12 L12,22 L2,12 Z M12,7 L17,12 L12,17 L7,12 Z",
            IconKind.Calculator => "M4,2 L20,2 L20,22 L4,22 Z M4,8 L20,8 M8,8 L8,22 M13,8 L13,22 M4,15 L20,15",
            IconKind.Layers => "M12,3 L21,8 L12,13 L3,8 Z M3,13 L12,18 L21,13 M3,17 L12,22 L21,17",
            IconKind.Globe => "M2,2 L22,2 L22,22 L2,22 Z M2,2 L22,22 M22,2 L2,22 M2,12 L22,12 M12,2 L12,22",
            IconKind.Settings => "M4,6 L20,6 M4,12 L20,12 M4,18 L20,18 M7,4 L7,8 L9,8 L9,4 Z M13,10 L13,14 L15,14 L15,10 Z M7,16 L7,20 L9,20 L9,16 Z",
            _ => "",
        });
        geometry.Freeze();
        return geometry;
    }
}
