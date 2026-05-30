using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic.OptionPanels;

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
    bool IsBackBufferStoredInVideoMemory { get; set; }
    bool IsBorderlessClientEnabled { get; set; }
    bool IsIntegerScaledClientEnabled { get; set; }
    bool IsGameCompatFixAvailable { get; }
    bool IsFinalSunCompatFixAvailable { get; }
    bool IsRestartRequired { get; }
    bool IsMessageBoxVisible { get; }
    string MessageBoxTitle { get; }
    string MessageBoxMessage { get; }

    IRelayCommand InstallGameCompatibilityFixCommand { get; }
    IRelayCommand InstallMapEditorCompatibilityFixCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
}
