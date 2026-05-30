using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer;

public interface ILANLobbyViewModel : INotifyPropertyChanged
{
    bool IsVisible { get; set; }
    IReadOnlyList<string> GameNames { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<string> ChatMessages { get; }
    IReadOnlyList<string> ColorOptions { get; }
    int SelectedGameIndex { get; set; }
    int SelectedColorIndex { get; set; }
    string PlayerName { get; }
    string DraftMessage { get; set; }
    bool IsEnabled { get; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand ExitLobbyCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }
}
