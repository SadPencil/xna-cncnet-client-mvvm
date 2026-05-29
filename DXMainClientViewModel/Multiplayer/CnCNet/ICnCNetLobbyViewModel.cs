using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;
using DXMainClientViewModel.Multiplayer.GameLobby;

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
    bool IsVisible { get; set; }
    IReadOnlyList<string> ColorOptions { get; }
    IReadOnlyList<string> ChannelOptions { get; }

    // View-reactive state
    string? PendingMessage { get; set; }
    PendingYesNoDialogData? PendingYesNoDialog { get; set; }
    bool IsUpdateCheckNeeded { get; set; }
    bool IsLoginWindowVisible { get; set; }
    bool IsGameCreationPanelVisible { get; set; }
    PendingGameInviteData? PendingGameInvite { get; set; }
    string? SoundToPlay { get; set; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand OpenPrivateMessagesCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
    IRelayCommand LogoutCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand CycleSortDirectionCommand { get; }
    IRelayCommand ToggleGameFiltersCommand { get; }
    IRelayCommand AcceptGameInviteCommand { get; }
    IRelayCommand DismissGameInviteCommand { get; }
    IRelayCommand AcceptUpdateCommand { get; }
    IRelayCommand DenyUpdateCommand { get; }

    // Lifecycle methods
    void Initialize();
    void SetGameLobbies(ICnCNetGameLobbyViewModel gameLobby, ICnCNetGameLoadingLobbyViewModel gameLoadingLobby);
    void SetPrivateMessagingWindow(PrivateMessagingWindowViewModel pmWindow);
    void SwitchOn();
    void SwitchOff();
    void Clean();

    // Coordination methods (called by parent MainMenu)
    void OnGameCreated(string gameRoomName, string channelName, string password, int maxPlayers, CnCNetTunnel tunnel, int skillLevel);
    void OnLoadedGameCreated(string gameRoomName, string channelName, string password, CnCNetTunnel tunnel);
    void OnPasswordEntered(HostedCnCNetGame game, string password);
    void OnGameLobbyLeft();
    void OnGameLoadingLobbyLeft();
    void OnGameFiltersPanelClosed();
}
