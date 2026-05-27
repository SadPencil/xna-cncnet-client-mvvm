#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IDisplayOptionsPanelViewModel
{
    IReadOnlyList<string> IngameResolutionOptions { get; }
    int SelectedIngameResolutionIndex { get; set; }
    IReadOnlyList<string> ClientResolutionOptions { get; }
    int SelectedClientResolutionIndex { get; set; }
    IReadOnlyList<string> DetailLevelOptions { get; }
    int SelectedDetailLevelIndex { get; set; }
    IReadOnlyList<string> RendererOptions { get; }
    int SelectedRendererIndex { get; set; }
    IReadOnlyList<string> ThemeOptions { get; }
    int SelectedThemeIndex { get; set; }
    IReadOnlyList<string> TranslationOptions { get; }
    int SelectedTranslationIndex { get; set; }
    bool IsWindowedModeEnabled { get; set; }
    bool IsBorderlessWindowedModeEnabled { get; set; }
    bool IsBackBufferStoredInVideoMemory { get; set; }
    bool IsBorderlessClientEnabled { get; set; }
    bool IsIntegerScaledClientEnabled { get; set; }

    IRelayCommand InstallGameCompatibilityFixCommand { get; }
    IRelayCommand InstallMapEditorCompatibilityFixCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
