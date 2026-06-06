#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore;

namespace AvClientViewModel.Services
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    public sealed record ScreenResolution : IComparable<ScreenResolution>
    {

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; }

        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public ScreenResolution(string resolution)
        {
            List<int> resolutionList = resolution.Trim().Split('x').Take(2).Select(int.Parse).ToList();
            Width = resolutionList[0];
            Height = resolutionList[1];
        }

        public static implicit operator ScreenResolution(string resolution) => new(resolution);

        public sealed override string ToString() => Width + "x" + Height;

        public static implicit operator string(ScreenResolution resolution) => resolution.ToString();

        public void Deconstruct(out int width, out int height)
        {
            width = this.Width;
            height = this.Height;
        }

        public static implicit operator ScreenResolution((int Width, int Height) resolutionTuple) => new(resolutionTuple.Width, resolutionTuple.Height);

        public static implicit operator (int Width, int Height)(ScreenResolution resolution) => new(resolution.Width, resolution.Height);

        public bool Fits(ScreenResolution child) => this.Width >= child.Width && this.Height >= child.Height;

        public int CompareTo(ScreenResolution? other)
        {
            if (other is null)
                return 1;
            return (this.Width, this.Height).CompareTo((other.Width, other.Height));
        }

        private static ScreenResolution? _desktopResolution;

        /// <summary>
        /// The resolution of the primary monitor. Defaults to 1280x720 until initialized
        /// by ResolutionProvider with the actual desktop resolution from the View layer.
        /// </summary>
        public static ScreenResolution DesktopResolution
        {
            get => _desktopResolution ??= new ScreenResolution(1280, 720);
            set => _desktopResolution = value;
        }

        private static ScreenResolution? _fullScreenResolution;

        /// <summary>
        /// The largest full screen resolution among available display modes, or the desktop resolution as fallback.
        /// </summary>
        public static ScreenResolution FullScreenResolution
        {
            get
            {
                if (_fullScreenResolution == null)
                {
                    var resolutions = GetFullScreenResolutions(minWidth: 800, minHeight: 600);
                    _fullScreenResolution = resolutions.Max ?? DesktopResolution;
                }

                return _fullScreenResolution;
            }
        }

        /// <summary>
        /// Comprehensive list of common monitor resolutions used when GPU display modes cannot be queried.
        /// Covers standard VESA/CEA/HDMI modes plus ultrawide and high-DPI resolutions.
        /// </summary>
        private static readonly IReadOnlyList<ScreenResolution> CommonFullScreenResolutions =
        [
            "640x480", "720x480", "720x576", "800x600", "1024x768",
            "1152x864", "1176x664", "1280x720", "1280x768", "1280x800",
            "1280x960", "1280x1024", "1360x768", "1366x768", "1400x1050",
            "1440x900", "1600x900", "1600x1024", "1600x1200", "1680x1050",
            "1768x992", "1920x1080", "1920x1200", "1920x1440", "2048x1152",
            "2048x1536", "2560x1080", "2560x1440", "2560x1600", "2880x1800",
            "3072x1728", "3200x1800", "3440x1440", "3840x2160", "4096x2160",
            "5120x2880", "6016x3384", "7680x4320",
        ];

        public static SortedSet<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight) =>
            GetFullScreenResolutions(minWidth, minHeight, DesktopResolution.Width, DesktopResolution.Height);
        public static SortedSet<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            SortedSet<ScreenResolution> screenResolutions = [];

            foreach (ScreenResolution res in CommonFullScreenResolutions)
            {
                if (res.Width < minWidth || res.Height < minHeight || res.Width > maxWidth || res.Height > maxHeight)
                    continue;

                screenResolutions.Add(res);
            }

            return screenResolutions;
        }

        public static readonly IReadOnlyList<ScreenResolution> OptimalWindowedResolutions = ["1280x720"];

        public const int MAX_INT_SCALE = 9;

        public SortedSet<ScreenResolution> GetIntegerScaledResolutions() =>
            GetIntegerScaledResolutions(DesktopResolution);
        public SortedSet<ScreenResolution> GetIntegerScaledResolutions(ScreenResolution maxResolution)
        {
            SortedSet<ScreenResolution> resolutions = [];
            for (int i = 1; i <= MAX_INT_SCALE; i++)
            {
                ScreenResolution scaledResolution = (this.Width * i, this.Height * i);

                if (maxResolution.Fits(scaledResolution))
                    resolutions.Add(scaledResolution);
                else
                    break;
            }

            return resolutions;
        }

        public static SortedSet<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight) =>
            GetWindowedResolutions(minWidth, minHeight, DesktopResolution.Width, DesktopResolution.Height);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(IEnumerable<ScreenResolution> optimalResolutions, int minWidth, int minHeight) =>
            GetWindowedResolutions(OptimalWindowedResolutions, minWidth, minHeight, DesktopResolution.Width, DesktopResolution.Height);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight) =>
            GetWindowedResolutions(OptimalWindowedResolutions, minWidth, minHeight, maxWidth, maxHeight);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(IEnumerable<ScreenResolution> optimalResolutions, int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            ScreenResolution maxResolution = (maxWidth, maxHeight);

            SortedSet<ScreenResolution> windowedResolutions = [];

            foreach (ScreenResolution optimalResolution in optimalResolutions)
            {
                if (optimalResolution.Width < minWidth || optimalResolution.Height < minHeight)
                    continue;

                if (!maxResolution.Fits(optimalResolution))
                    continue;

                windowedResolutions.Add(optimalResolution);
            }

            return windowedResolutions;
        }

        public static SortedSet<ScreenResolution> GetRecommendedResolutions()
        {
            List<ScreenResolution> recommendedResolutions = ClientConfiguration.Instance.RecommendedResolutions.Select(resolution => (ScreenResolution)resolution).ToList();
            SortedSet<ScreenResolution> scaledRecommendedResolutions = [.. recommendedResolutions.SelectMany(resolution => resolution.GetIntegerScaledResolutions())];
            return scaledRecommendedResolutions;
        }

        public static SortedSet<ScreenResolution> GetCustomIngameResolutions()
        {
            var customIngameResolutions = ClientConfiguration.Instance.CustomIngameResolutions
                .Where(resolution => !string.IsNullOrWhiteSpace(resolution))
                .Select(resolution => (ScreenResolution)resolution)
                .ToList();

            var sortedCustomIngameResolutions = new SortedSet<ScreenResolution>(customIngameResolutions);
            return sortedCustomIngameResolutions;
        }

        public static ScreenResolution GetBestRecommendedResolution() =>
            GetRecommendedResolutions().Max ?? FullScreenResolution;

    }
}
