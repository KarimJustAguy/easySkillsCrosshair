using System.Globalization;
using System.Windows.Data;

namespace easySkillsCrosshair.App.Converters;

/// <summary>True when all bound values are the same object — used to highlight the selected item in a button list.</summary>
public sealed class ReferenceEqualsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Length >= 2 && values.Skip(1).All(v => ReferenceEquals(v, values[0]));

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
