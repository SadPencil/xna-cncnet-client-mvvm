#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ILANLobbyViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> GameNames { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<string> ChatMessages { get; }
    IReadOnlyList<string> ColorOptions { get; }
    int SelectedGameIndex { get; set; }
    int SelectedColorIndex { get; set; }
    string PlayerName { get; set; }
    string DraftMessage { get; set; }

    IRelayCommand CreateGameCommand { get; }
    IRelayCommand JoinSelectedGameCommand { get; }
    IRelayCommand ExitLobbyCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand RefreshGamesCommand { get; }

    void Initialize();
}
