namespace easySkillsCrosshair.Core.Crosshair;

public enum LayerType
{
    /// <summary>Four arms with length/thickness/gap; <see cref="CrosshairLayer.TStyle"/> drops the top arm.</summary>
    Cross,
    Dot,
    Circle,
    /// <summary>Two diagonal arms rotated 45° — an "X".</summary>
    XCross,
    /// <summary>Square outline.</summary>
    Box,
    /// <summary>Upward-pointing "^".</summary>
    Chevron,
    /// <summary>Single horizontal line (rotate the layer for other angles).</summary>
    Line,
    /// <summary>User-supplied PNG/GIF/SVG reticle.</summary>
    Image,
}
