#nullable enable

using System.Collections.Generic;

namespace DXMainClientView;

public interface IDisplayOptionsPanelView
{
    void SetIngameResolutionOptions(IEnumerable<string> options);
    void SetSelectedIngameResolution(string option);
    void SetRendererOptions(IEnumerable<string> options);
    void SetSelectedRenderer(string option);
    void SetClientThemeOptions(IEnumerable<string> options);
    void SetSelectedClientTheme(string option);
    void SetClientResolutionOptions(IEnumerable<string> options);
    void SetSelectedClientResolution(string option);
    void SetTranslationOptions(IEnumerable<string> options);
    void SetSelectedTranslation(string option);
    void SetWindowedMode(bool enabled);
    void SetBorderlessMode(bool enabled);
    void SetIntegerScaling(bool enabled);
    void SetCompatibilityStatus(string statusText);
}
