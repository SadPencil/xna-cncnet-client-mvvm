using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IGameCreationWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    string Password { get; set; }
    int MaxPlayers { get; set; }
    bool IsPrivateGame { get; set; }
    bool IsLoadedGame { get; }
    int SelectedTunnelIndex { get; set; }
    int SelectedSkillLevel { get; set; }
    bool IsAdvancedOptionsVisible { get; set; }
    bool CanCreateGame { get; }
    bool CanLoadGame { get; }
    string ValidationErrorMessage { get; }

    IReadOnlyList<string> TunnelNames { get; }
    IReadOnlyList<string> MaxPlayersOptions { get; }
    IReadOnlyList<string> SkillLevelOptions { get; }

    IAsyncRelayCommand CreateGameCommand { get; }
    IAsyncRelayCommand CreateLoadedGameCommand { get; }
    IRelayCommand OpenAdvancedOptionsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
