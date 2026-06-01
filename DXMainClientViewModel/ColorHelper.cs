using System;
using System.Globalization;

using DXMainClientViewModel.Online;

namespace DXMainClientViewModel
{
    /// <summary>
    /// Helper for parsing color strings. Replaces AssetLoader.GetColorFromString.
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// Parses a color string in "R,G,B" or "R,G,B,A" format and returns an Rgb24Color.
        /// </summary>
        public static Rgb24Color GetRgbFromString(string colorString)
        {
            if (string.IsNullOrWhiteSpace(colorString))
            {
                return new Rgb24Color(255, 255, 255);
            }

            string[] parts = colorString.Split(',');
            if (parts.Length >= 3)
            {
                byte r = (byte)Math.Min(255, int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture));
                byte g = (byte)Math.Min(255, int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
                byte b = (byte)Math.Min(255, int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
                return new Rgb24Color(r, g, b);
            }
            else
            {
                return new Rgb24Color(255, 255, 255);
            }
        }
    }
}
