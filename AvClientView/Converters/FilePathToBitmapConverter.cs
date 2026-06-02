using System;
using System.Globalization;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace AvClientView.Converters;

/// <summary>
/// Converts a file path string to an Avalonia Bitmap for use as Image.Source.
/// Returns null if the path is empty or the file doesn't exist.
/// </summary>
public class FilePathToBitmapConverter : IValueConverter
{
    public static readonly FilePathToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            try
            {
                return new Bitmap(path);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
