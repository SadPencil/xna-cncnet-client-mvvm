using System.Collections.Generic;
using DXMainClientViewModel.Services;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IResolutionProvider for the View layer.
/// Returns common resolutions.
/// </summary>
public class StubResolutionProvider : IResolutionProvider
{
    private static readonly List<string> Resolutions = new()
    {
        "800x600",
        "1024x768",
        "1280x720",
        "1280x1024",
        "1366x768",
        "1600x900",
        "1920x1080",
        "2560x1440",
        "3840x2160"
    };

    public IReadOnlyList<string> GetIngameResolutions() => Resolutions;
    public IReadOnlyList<string> GetClientResolutions() => Resolutions;
    public string GetSafeFullScreenResolution() => "1920x1080";
    public string GetBestRecommendedResolution() => "1280x720";
}
