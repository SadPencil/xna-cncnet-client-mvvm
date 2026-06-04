using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace AvClientView.Converters;

/// <summary>
/// Provides static boolean converter instances for use with compiled bindings.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// Negates a boolean value. Returns null if the value is not a bool.
    /// </summary>
    public static readonly IValueConverter Not = new BoolNotConverter();
}

internal sealed class BoolNotConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return null;
    }
}
