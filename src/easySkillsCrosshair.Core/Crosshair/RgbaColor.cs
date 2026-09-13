namespace easySkillsCrosshair.Core.Crosshair;

/// <summary>
/// UI-framework-agnostic color value (no System.Windows.Media dependency), so the
/// domain model in Core stays usable outside WPF.
/// </summary>
public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    public static RgbaColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch
        {
            6 => new RgbaColor(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16)),
            8 => new RgbaColor(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16)),
            _ => throw new FormatException($"'{hex}' is not a valid hex color."),
        };
    }

    public static bool TryFromHex(string? hex, out RgbaColor color)
    {
        if (hex is not null)
        {
            try
            {
                color = FromHex(hex);
                return true;
            }
            catch (FormatException)
            {
            }
        }

        color = default;
        return false;
    }

    public string ToHex() => A == 255 ? $"#{R:X2}{G:X2}{B:X2}" : $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}
