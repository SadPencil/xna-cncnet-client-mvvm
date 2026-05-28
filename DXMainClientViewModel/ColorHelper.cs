using System;
using System.Globalization;

namespace DXMainClientViewModel
{
    /// <summary>
    /// Helper for parsing color strings. Replaces AssetLoader.GetColorFromString.
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// Parses a color string in "R,G,B" or "R,G,B,A" format and returns R, G, B values.
        /// </summary>
        public static void GetRgbFromString(string colorString, out int r, out int g, out int b)
        {
            if (string.IsNullOrWhiteSpace(colorString))
            {
                r = 255; g = 255; b = 255;
                return;
            }

            string[] parts = colorString.Split(',');
            if (parts.Length >= 3)
            {
                r = Math.Min(255, int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture));
                g = Math.Min(255, int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
                b = Math.Min(255, int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
            }
            else
            {
                r = 255; g = 255; b = 255;
            }
        }
    }
}
