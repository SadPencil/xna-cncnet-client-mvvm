using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using AvClientMvvmContract.Multiplayer.GameLobby;
using AvClientMvvmContract.Online;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer;

public interface ILANLobbyViewModel : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    ReadOnlyObservableCollection<ILANHostedGame> Games { get; }
    ReadOnlyObservableCollection<string> PlayerNames { get; }
    IReadOnlyList<IChatMessage> ChatMessages { get; }
    ReadOnlyObservableCollection<string> ColorOptions { get; }
    int SelectedGameIndex { get; set; }
    int SelectedChatMessageIndex { get; set; }
    int HoveredGameIndex { get; set; }
    int SelectedColorIndex { get; set; }
    string PlayerName { get; }
    string DraftMessage { get; set; }
    bool IsNewGameButtonEnabled { get; }
    bool IsJoinGameButtonEnabled { get; }
    bool IsChatInputEnabled { get; }
    bool IsGameListEnabled { get; }
    bool IsPlayerListEnabled { get; }
    bool IsColorDropdownEnabled { get; }

    ILANGameCreationWindowViewModel? GameCreationWindow { get; }
    ILANGameLobbyViewModel GameLobby { get; }
    ILANGameLoadingLobbyViewModel GameLoadingLobby { get; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand ExitLobbyCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
    IRelayCommand ChatMessageDoubleClickCommand { get; }
}
