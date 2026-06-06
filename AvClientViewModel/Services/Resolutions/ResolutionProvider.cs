using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ClientCore;

using Serilog;

namespace AvClientViewModel.Services.Resolutions
{
    /// <summary>
    /// Provides screen resolution options using ScreenResolution logic.
    /// Initializes ScreenResolution with the actual desktop resolution and
    /// display modes from the first applicable IScreenInfoProvider.
    /// </summary>
    internal class ResolutionProvider : IResolutionProvider
    {
        public ResolutionProvider(IEnumerable<IScreenInfoProvider> screenInfoProviders)
        {
            var providers = screenInfoProviders.ToList();

            // Run IsApplicable() checks in parallel
            var results = new (IScreenInfoProvider provider, bool applicable)[providers.Count];
            Parallel.For(0, providers.Count, i =>
            {
                results[i] = (providers[i], providers[i].IsApplicable());
            });

            // Pick the highest-priority applicable provider with non-empty modes
            IScreenInfoProvider? selected = null;

            foreach (var (provider, applicable) in results.OrderByDescending(r => r.provider.Priority))
            {
                if (!applicable)
                    continue;

                var modes = provider.GetSupportedDisplayModes();
                if (modes.Count == 0)
                    continue;

                selected = provider;
                break;
            }

            // Fall back to first applicable (dummy always applies)
            if (selected == null)
            {
                foreach (var (provider, applicable) in results.OrderByDescending(r => r.provider.Priority))
                {
                    if (applicable) { selected = provider; break; }
                }
            }

            ScreenResolution.DesktopResolution = new ScreenResolution(
                selected!.DesktopWidth,
                selected!.DesktopHeight);

            ScreenResolution.DisplayModes = selected.GetSupportedDisplayModes()
                .Select(m => new ScreenResolution(m.Width, m.Height))
                .ToList();
        }

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
