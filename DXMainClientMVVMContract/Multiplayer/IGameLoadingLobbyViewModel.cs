using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface IGameLoadingLobbyViewModel : INotifyPropertyChanged
{
    string GameName { get; }
    string MapName { get; }
    string GameMode { get; }
    string HostName { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<IPlayerDisplayInfo> PlayerDisplayInfo { get; }
    IReadOnlyList<string> ChatMessages { get; }
    IReadOnlyList<string> SavedGameNames { get; }
    int SelectedSavedGameIndex { get; set; }
    bool IsHost { get; }
    bool CanLoadGame { get; }
    string LoadGameButtonText { get; }
    string DraftMessage { get; set; }

    IRelayCommand LoadGameCommand { get; }
    IRelayCommand LeaveGameCommand { get; }
    IRelayCommand SendChatMessageCommand { get; }
}
