#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameLobbyViewModel
{
    string GameName { get; }
    string MapName { get; }
    string GameModeName { get; }
    IReadOnlyList<string> PlayerNames { get; }
    int SelectedPlayerIndex { get; set; }
    bool IsHost { get; }
    bool CanLaunchGame { get; }

    IRelayCommand LeaveGameCommand { get; }
    IAsyncRelayCommand LaunchGameCommand { get; }
    IRelayCommand OpenGameSettingsCommand { get; }
    IRelayCommand OpenMapSelectionCommand { get; }

    void Initialize();
}
