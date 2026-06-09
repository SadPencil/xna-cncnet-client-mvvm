using System.Collections.Generic;
using System.ComponentModel;

using AvClientMvvmContract.Multiplayer.GameLobby;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer;

public interface ILANLobbyViewModel : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    IReadOnlyList<ILANHostedGame> Games { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<string> ChatMessages { get; }
    IReadOnlyList<string> ColorOptions { get; }
    int SelectedGameIndex { get; set; }
    int SelectedColorIndex { get; set; }
    string PlayerName { get; }
    string DraftMessage { get; set; }
    bool IsEnabled { get; }

    ILANGameCreationWindowViewModel? GameCreationWindow { get; }
    ILANGameLobbyViewModel GameLobby { get; }
    ILANGameLoadingLobbyViewModel GameLoadingLobby { get; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand ExitLobbyCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
}
