using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace AvClientView.Converters;

/// <summary>
/// Provides static string converter instances for use with compiled bindings.
/// </summary>
public static class StringConverters
{
    /// <summary>
    /// Returns true if the value is a non-null, non-empty string.
    /// </summary>
    public static readonly IValueConverter IsNotNullOrEmpty = new IsNotNullOrEmptyConverter();
}

internal sealed class IsNotNullOrEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrEmpty(s);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
