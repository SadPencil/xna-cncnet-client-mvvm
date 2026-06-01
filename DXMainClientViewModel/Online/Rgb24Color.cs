using System;
using System.Globalization;

using DXMainClientMvvmContract;

namespace DXMainClientViewModel.Online;

public readonly record struct Rgb24Color(byte R, byte G, byte B) : IRgb24Color
{
    public static readonly Rgb24Color White = new(255, 255, 255);
    public static readonly Rgb24Color Red = new(255, 0, 0);
    public static readonly Rgb24Color Yellow = new(255, 255, 0);
    public static readonly Rgb24Color Gray = new(128, 128, 128);

    /// <summary>
    /// Parses a color string in "R,G,B" or "R,G,B,A" format, dropping the alpha component if present. Each component should be an integer between 0 and 255.
    /// </summary>
    public static Rgb24Color FromString(string colorString)
    {
        if (string.IsNullOrWhiteSpace(colorString))
            throw new ArgumentException("Color string must not be empty.", nameof(colorString));

        string[] parts = colorString.Split(',');
        if (parts.Length < 3)
            throw new FormatException($"Color string '{colorString}' is not in 'R,G,B' format.");

        byte r = byte.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
        byte g = byte.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
        byte b = byte.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
        return new Rgb24Color(r, g, b);
    }
}
