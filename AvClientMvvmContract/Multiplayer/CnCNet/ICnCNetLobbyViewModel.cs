using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

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
    bool IsVisible { get; }
    IReadOnlyList<string> ColorOptions { get; }
    IReadOnlyList<string> ChannelOptions { get; }

    // View-reactive state
    string? PendingMessage { get; }
    IPendingYesNoDialogData? PendingYesNoDialog { get; }
    bool IsUpdateCheckNeeded { get; }
    bool IsLoginWindowVisible { get; }
    ICnCNetLoginWindowViewModel LoginWindowViewModel { get; }
    bool IsGameCreationPanelVisible { get; }
    IPendingGameInviteData? PendingGameInvite { get; }
    string? SoundToPlay { get; }

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
    IRelayCommand DismissMessageCommand { get; }
}
