using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media;

using AvClientMvvmContract.Domain.Multiplayer;

namespace AvClientView.Converters;

/// <summary>
/// Returns a gray SolidColorBrush if the IHostedCnCNetGame is Locked or Incompatible,
/// otherwise returns the fallback brush provided as the converter parameter (or default white).
///
/// Usage: Foreground="{Binding ., Converter={x:Static converters:GameStateToBrushConverter.Instance},
///                         ConverterParameter={DynamicResource XnaTextBrush}}"
/// </summary>
public class GameStateToBrushConverter : IValueConverter
{
    public static readonly GameStateToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IHostedCnCNetGame game && (game.Locked || game.Incompatible))
            return Brushes.Gray;

        // Use the fallback brush if provided
        if (parameter is IBrush brush)
            return brush;

        return Brushes.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
