using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.App.Converters;

/// <summary>Renders a hex-color swatch next to the hex TextBox in the color picker.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && RgbaColor.TryFromHex(hex, out var color))
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }

        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
