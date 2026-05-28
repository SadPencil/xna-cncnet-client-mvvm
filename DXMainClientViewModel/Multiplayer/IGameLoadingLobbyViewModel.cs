using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer;

public interface IGameLoadingLobbyViewModel : INotifyPropertyChanged
{
    string GameName { get; }
    string MapName { get; }
    string HostName { get; }
    IReadOnlyList<string> PlayerNames { get; }
    IReadOnlyList<string> ChatMessages { get; }
    IReadOnlyList<string> SavedGameNames { get; }
    int SelectedSavedGameIndex { get; set; }
    bool IsHost { get; }
    bool CanLoadGame { get; }

    IRelayCommand LoadGameCommand { get; }
    IRelayCommand LeaveGameCommand { get; }
    IRelayCommand<string> SendChatMessageCommand { get; }

    void Initialize();
}
