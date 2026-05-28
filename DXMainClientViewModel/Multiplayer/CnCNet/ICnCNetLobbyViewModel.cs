using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ICnCNetLobbyViewModel : INotifyPropertyChanged
{
    string CurrentChannelName { get; }
    string OnlinePlayerCountText { get; }
    string PlayerName { get; }
    string DraftMessage { get; set; }
    IReadOnlyList<string> GameNames { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<string> ChatMessages { get; }
    int SelectedGameIndex { get; set; }
    int SelectedColorIndex { get; set; }
    int SelectedChannelIndex { get; set; }
    string GameSearchText { get; set; }
    string LogoutButtonText { get; }
    bool IsNewGameButtonEnabled { get; }
    bool IsJoinGameButtonEnabled { get; }
    bool IsChatInputEnabled { get; }
    bool IsChannelDropdownEnabled { get; }
    bool IsGameSearchEnabled { get; }
    bool IsConnected { get; }
    IReadOnlyList<string> ColorOptions { get; }
    IReadOnlyList<string> ChannelOptions { get; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand OpenPrivateMessagesCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
    IRelayCommand LogoutCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand CycleSortDirectionCommand { get; }
    IRelayCommand ToggleGameFiltersCommand { get; }

    event System.Action? MessageBoxRequested;
    event System.Action<string, string, System.Action<bool>>? YesNoDialogRequested;
    event System.Action? SwitchToPrimaryRequested;
    event System.Action? SwitchToSecondaryRequested;
    event System.Action? UpdateCheckRequested;
    event System.Action? LoginWindowRequested;
    event System.Action? GameCreationPanelShowRequested;
    event System.Action? GameCreationPanelHideRequested;
    event System.Action<string, string, string>? GameInviteReceived;
    event System.Action<string>? SoundPlayRequested;

    void Initialize();
    void SwitchOn();
    void SwitchOff();
    void Clean();
}
