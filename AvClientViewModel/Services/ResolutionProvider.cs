using System.Collections.Generic;
using System.Linq;

using ClientCore;

using Serilog;

namespace AvClientViewModel.Services
{
    /// <summary>
    /// Provides screen resolution options using ScreenResolution logic.
    /// Uses comprehensive common resolution lists and INI configuration,
    /// matching the behavior of the original XNA client without GPU queries.
    /// </summary>
    public class ResolutionProvider : IResolutionProvider
    {
        public IReadOnlyList<string> GetIngameResolutions()
        {
            var maximumIngameResolution = new ScreenResolution(
                ClientConfiguration.Instance.MaximumIngameWidth,
                ClientConfiguration.Instance.MaximumIngameHeight);

            SortedSet<ScreenResolution> resolutions = ScreenResolution.GetFullScreenResolutions(
                ClientConfiguration.Instance.MinimumIngameWidth,
                ClientConfiguration.Instance.MinimumIngameHeight,
                maximumIngameResolution.Width,
                maximumIngameResolution.Height);

            var minimumIngameResolution = new ScreenResolution(
                ClientConfiguration.Instance.MinimumIngameWidth,
                ClientConfiguration.Instance.MinimumIngameHeight);

            // Add custom in-game resolutions
            var customIngameResolutions = ScreenResolution.GetCustomIngameResolutions();
            foreach (var customRes in customIngameResolutions)
            {
                if (!customRes.Fits(minimumIngameResolution))
                {
                    Log.Warning(
                        $"Custom in-game resolution {customRes} is too small. " +
                        "Please check 'MinimumIngameWidth' and 'MinimumIngameHeight' in 'ClientDefinitions.ini' file.");
                }

                if (!maximumIngameResolution.Fits(customRes))
                {
                    Log.Warning(
                        $"Custom in-game resolution {customRes} is too large. " +
                        "Please check 'MaximumIngameWidth' and 'MaximumIngameHeight' in 'ClientDefinitions.ini' file.");
                }

                resolutions.Add(customRes);
            }

            return resolutions.Select(r => r.ToString()).ToList();
        }

        public IReadOnlyList<string> GetClientResolutions()
        {
            SortedSet<ScreenResolution> scaledRecommendedResolutions = ScreenResolution.GetRecommendedResolutions();

            SortedSet<ScreenResolution> resolutions =
            [
                .. ScreenResolution.GetFullScreenResolutions(minWidth: 800, minHeight: 600),
                .. ScreenResolution.GetWindowedResolutions(minWidth: 800, minHeight: 600),
                .. scaledRecommendedResolutions,
            ];

            return resolutions.Select(r => r.ToString()).ToList();
        }

        public string GetSafeFullScreenResolution()
        {
            return ScreenResolution.FullScreenResolution;
        }

        public string GetBestRecommendedResolution()
        {
            return ScreenResolution.GetBestRecommendedResolution();
        }
    }
}
