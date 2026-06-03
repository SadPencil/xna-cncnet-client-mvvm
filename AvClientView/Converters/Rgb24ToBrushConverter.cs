using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using AvClientMvvmContract;

namespace AvClientView.Converters;

/// <summary>
/// Converts an IRgb24Color to an Avalonia SolidColorBrush for use as Foreground.
/// </summary>
public class Rgb24ToBrushConverter : IValueConverter
{
    public static readonly Rgb24ToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IRgb24Color c)
            return new SolidColorBrush(Color.FromArgb(255, c.R, c.G, c.B));
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
