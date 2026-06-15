using System.Collections.Generic;
using System.ComponentModel;

using AvClientMvvmContract.Domain.Multiplayer;
using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.Online;
using AvClientMvvmContract.ViewServices;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

public interface ICnCNetLobbyViewModel : INotifyPropertyChanged
{
    string CurrentChannelName { get; }
    string OnlinePlayerCountText { get; }
    string PlayerName { get; }
    string DraftMessage { get; set; }
    IReadOnlyList<IHostedCnCNetGame> Games { get; }
    IReadOnlyList<IPlayerListItem> Players { get; }
    IReadOnlyList<IChatMessage> ChatMessages { get; }
    IHostedCnCNetGame? SelectedGame { get; }
    int SelectedGameIndex { get; set; }
    int HoveredGameIndex { get; set; }
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
    IGameCreationWindowViewModel? GameCreationWindowViewModel { get; }
    ITunnelSelectionWindowViewModel? TunnelSelectionWindowViewModel { get; }
    IPasswordRequestWindowViewModel? PasswordRequestWindowViewModel { get; }
    IReadOnlyList<IIRCColor> ColorOptions { get; }
    IReadOnlyList<string> ChannelOptions { get; }

    // View-reactive state
    string? PendingMessage { get; }
    bool IsUpdateCheckNeeded { get; }
    bool IsLoginWindowVisible { get; }
    ICnCNetLoginWindowViewModel LoginWindowViewModel { get; }
    bool IsGameCreationPanelVisible { get; }
    bool IsTunnelSelectionVisible { get; }
    bool IsPasswordRequestVisible { get; }
    IPendingGameInviteData? PendingGameInvite { get; }
    string? SoundToPlay { get; }

    ICnCNetGameLobbyViewModel GameLobby { get; }
    ICnCNetGameLoadingLobbyViewModel GameLoadingLobby { get; }

    int SelectedPlayerIndex { get; set; }
    int SelectedChatMessageIndex { get; set; }

    string? PendingLink { get; set; }

    IReadOnlyList<IContextMenuItem> PlayerContextMenuItems { get; }
    IReadOnlyList<IContextMenuItem> ChatContextMenuItems { get; }
    IReadOnlyList<IContextMenuItem> GameContextMenuItems { get; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
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
    IRelayCommand OpenSelectedPlayerPrivateMessageCommand { get; }
    IRelayCommand ToggleSelectedPlayerFriendCommand { get; }
    IRelayCommand ToggleSelectedPlayerIgnoreCommand { get; }
    IRelayCommand InviteSelectedPlayerToGameCommand { get; }
    IRelayCommand JoinSelectedPlayerGameCommand { get; }
    IRelayCommand OpenSelectedChatMessageSenderPrivateMessageCommand { get; }
    IRelayCommand ToggleSelectedChatMessageSenderFriendCommand { get; }
    IRelayCommand ToggleSelectedChatMessageSenderIgnoreCommand { get; }
    IRelayCommand JoinSelectedChatMessageSenderGameCommand { get; }
    IRelayCommand OpenPendingLinkCommand { get; }
    IRelayCommand CopyPendingLinkCommand { get; }
    IRelayCommand OpenSelectedGameHostPrivateMessageCommand { get; }
    IRelayCommand ToggleSelectedGameHostFriendCommand { get; }
    IRelayCommand ToggleSelectedGameHostIgnoreCommand { get; }
    IRelayCommand ChatMessageDoubleClickCommand { get; }
}
