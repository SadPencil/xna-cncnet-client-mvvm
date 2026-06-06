using System;
using System.Globalization;

using Avalonia.Data.Converters;

namespace AvClientView.Converters;

/// <summary>
/// Provides a static converter to map AvClientMvvmContract.Generic.WindowState to Avalonia.Controls.WindowState.
/// </summary>
public static class WindowStateConverters
{
    public static readonly IValueConverter ToAvalonia = new WindowStateToAvaloniaConverter();
}

internal sealed class WindowStateToAvaloniaConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AvClientMvvmContract.Generic.WindowState state)
        {
            return state switch
            {
                AvClientMvvmContract.Generic.WindowState.Minimized => Avalonia.Controls.WindowState.Minimized,
                AvClientMvvmContract.Generic.WindowState.Maximized => Avalonia.Controls.WindowState.Maximized,
                AvClientMvvmContract.Generic.WindowState.FullScreen => Avalonia.Controls.WindowState.FullScreen,
                _ => Avalonia.Controls.WindowState.Normal
            };
        }
        return Avalonia.Controls.WindowState.Normal;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Avalonia.Controls.WindowState state)
        {
            return state switch
            {
                Avalonia.Controls.WindowState.Minimized => AvClientMvvmContract.Generic.WindowState.Minimized,
                Avalonia.Controls.WindowState.Maximized => AvClientMvvmContract.Generic.WindowState.Maximized,
                Avalonia.Controls.WindowState.FullScreen => AvClientMvvmContract.Generic.WindowState.FullScreen,
                _ => AvClientMvvmContract.Generic.WindowState.Normal
            };
        }
        return AvClientMvvmContract.Generic.WindowState.Normal;
    }
}
