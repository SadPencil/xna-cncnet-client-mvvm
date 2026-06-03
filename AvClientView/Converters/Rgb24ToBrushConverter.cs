using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using AvClientMvvmContract;

using Serilog;

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
        {
            var brush = new SolidColorBrush(Color.FromArgb(255, c.R, c.G, c.B));
            Log.Debug("[LOG-RGB24Conv] Convert: R={R}, G={G}, B={B}, type={T} -> #{X2:X2}{X2:X2}{X2:X2}",
                c.R, c.G, c.B, value.GetType().FullName,
                brush.Color.R, brush.Color.G, brush.Color.B);
            return brush;
        }
        Log.Debug("[LOG-RGB24Conv] Convert: value is NOT IRgb24Color, type={T}, value={V}",
            value?.GetType().FullName ?? "null", value);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
