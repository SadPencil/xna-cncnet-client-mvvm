using System;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.GameLobby;

public interface ISkirmishLobbyViewModel : IGameLobbyViewModel
{
    bool ShowPlayerNamesInGame { get; set; }

    IRelayCommand AddAiPlayerCommand { get; }
    IRelayCommand RemoveSelectedPlayerCommand { get; }
    IRelayCommand RandomizeSidesCommand { get; }
    IRelayCommand LaunchGameCommand { get; }
    IRelayCommand LeaveGameCommand { get; }

    void Initialize();

    event EventHandler Exited;
    event EventHandler<string> GameValidationErrorMessage;
    event EventHandler<NoticeEventArgs> NoticePosted;
    event EventHandler SettingsLoaded;
}
