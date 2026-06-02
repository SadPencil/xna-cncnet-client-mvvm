using System.Collections.Generic;
using System.Linq;

using ClientCore;

namespace AvMainClientViewModel.Services
{
    /// <summary>
    /// Provides screen resolution options without XNA GraphicsAdapter dependency.
    /// Uses ClientConfiguration for custom/rescommended resolutions and common defaults.
    /// </summary>
    public class ResolutionProvider : IResolutionProvider
    {
        private static readonly IReadOnlyList<string> CommonFullResolutions = new[]
        {
            "800x600", "1024x768", "1152x864", "1280x720", "1280x768", "1280x800",
            "1280x960", "1280x1024", "1360x768", "1366x768", "1440x900",
            "1600x900", "1600x1024", "1680x1050", "1920x1080", "1920x1200",
            "2560x1080", "2560x1440", "3840x2160"
        };

        private static readonly IReadOnlyList<string> CommonWindowedResolutions = new[]
        {
            "800x600", "1024x600", "1024x720", "1024x768",
            "1280x600", "1280x720", "1280x768", "1280x800", "1280x960", "1280x1024",
            "1360x768", "1366x768", "1440x900",
            "1600x900", "1680x1050", "1920x1080"
        };

        public IReadOnlyList<string> GetIngameResolutions()
        {
            var customResolutions = ClientConfiguration.Instance.CustomIngameResolutions
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            if (customResolutions.Count > 0)
                return customResolutions;

            var recommended = ClientConfiguration.Instance.RecommendedResolutions
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            if (recommended.Count > 0)
            {
                var expanded = new List<string>();
                foreach (var res in recommended)
                {
                    var parts = res.Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                    {
                        for (int scale = 1; scale <= 4; scale++)
                            expanded.Add($"{w * scale}x{h * scale}");
                    }
                }
                return expanded;
            }

            return CommonFullResolutions;
        }

        public IReadOnlyList<string> GetClientResolutions()
        {
            return CommonWindowedResolutions;
        }

        public string GetSafeFullScreenResolution()
        {
            return CommonFullResolutions[^1];
        }

        public string GetBestRecommendedResolution()
        {
            var recommended = ClientConfiguration.Instance.RecommendedResolutions
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            if (recommended.Count > 0)
            {
                string best = recommended[0];
                foreach (var res in recommended)
                {
                    var parts = res.Split('x');
                    var bestParts = best.Split('x');
                    if (parts.Length == 2 && bestParts.Length == 2
                        && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)
                        && int.TryParse(bestParts[0], out int bw) && int.TryParse(bestParts[1], out int bh))
                    {
                        if (w * h > bw * bh)
                            best = res;
                    }
                }
                return best;
            }

            return "1920x1080";
        }
    }
}
