#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameOptionsPanelViewModel
{
    int ScrollRate { get; set; }
    bool IsScrollCoastingEnabled { get; set; }
    bool AreTargetLinesEnabled { get; set; }
    bool AreTooltipsEnabled { get; set; }
    bool AreHiddenObjectsVisible { get; set; }
    bool IsBlackChatBackgroundEnabled { get; set; }
    bool IsUndeployWithAltEnabled { get; set; }
    string PlayerName { get; set; }

    IRelayCommand OpenHotkeyConfigurationCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
