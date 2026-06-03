using System;
using System.Globalization;

using Avalonia.Data.Converters;

using AvClientMvvmContract.Online;

namespace AvClientView.Converters;

/// <summary>
/// Formats an IChatMessage to a display string: "[HH:mm] senderName: message"
/// or "[HH:mm] message" for system messages with no sender.
/// </summary>
public class ChatMessageFormatterConverter : IValueConverter
{
    public static readonly ChatMessageFormatterConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IChatMessage msg)
            return null;

        string timestamp = msg.DateTime.ToShortTimeString();
        if (string.IsNullOrEmpty(msg.SenderName))
            return $"[{timestamp}] {msg.Message}";
        return $"[{timestamp}] {msg.SenderName}: {msg.Message}";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
