using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic.OptionPanels;

public interface IDisplayOptionsPanelViewModel : INotifyPropertyChanged
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
    bool IsBorderlessWindowedModeAllowed { get; }
    bool IsBackBufferStoredInVideoMemory { get; set; }
    bool IsBorderlessClientEnabled { get; set; }
    // Note: the IntegerScaledClient is no longer meaningful with the new renderers. We need to remove this option in the end.
    bool IsIntegerScaledClientEnabled { get; set; }
    bool IsGameCompatFixAvailable { get; }
    bool IsFinalSunCompatFixAvailable { get; }
    bool IsRestartRequired { get; }

    IRelayCommand InstallGameCompatibilityFixCommand { get; }
    IRelayCommand InstallMapEditorCompatibilityFixCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
