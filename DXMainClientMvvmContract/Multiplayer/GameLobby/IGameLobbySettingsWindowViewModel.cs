using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

public interface IGameLobbySettingsWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    string Password { get; set; }
    int MaxPlayers { get; set; }
    IReadOnlyList<string> MaxPlayerOptions { get; }
    IReadOnlyList<string> SkillLevelNames { get; }
    int SelectedSkillLevelIndex { get; set; }
    bool IsWindowVisible { get; set; }

    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
