using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace AvClientView.Converters;

/// <summary>
/// Provides static object converter instances for use with compiled bindings.
/// </summary>
public static class ObjectConverters
{
    /// <summary>
    /// Returns true if the value is not null.
    /// </summary>
    public static readonly IValueConverter IsNotNull = new IsNotNullConverter();
}

internal sealed class IsNotNullConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
