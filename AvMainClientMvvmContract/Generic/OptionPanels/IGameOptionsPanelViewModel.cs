using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Generic.OptionPanels;

public interface IGameOptionsPanelViewModel : INotifyPropertyChanged
{
    int ScrollRate { get; set; }
    bool IsScrollCoastingEnabled { get; set; }
    bool AreTargetLinesEnabled { get; set; }
    bool AreTooltipsEnabled { get; set; }
    bool AreHiddenObjectsVisible { get; set; }
    bool IsBlackChatBackgroundEnabled { get; set; }
    bool IsUndeployWithAltEnabled { get; set; }
    string PlayerName { get; set; }
    bool ShowHotkeyConfiguration { get; set; }

    IRelayCommand OpenHotkeyConfigurationCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
