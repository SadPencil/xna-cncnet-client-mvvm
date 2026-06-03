using System;
using System.Globalization;
using System.IO;

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

using SixLabors.ImageSharp;

namespace AvClientView.Converters;

/// <summary>
/// Converts a SixLabors.ImageSharp Image to an Avalonia Bitmap.
/// Renders the image to a PNG stream and loads it as a Bitmap.
/// </summary>
public class ImageSharpToBitmapConverter : IValueConverter
{
    public static readonly ImageSharpToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SixLabors.ImageSharp.Image image)
        {
            try
            {
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                ms.Position = 0;
                return new Bitmap(ms);
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
