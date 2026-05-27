#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface IGameCreationWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    string Password { get; set; }
    int MaxPlayers { get; set; }
    bool IsPrivateGame { get; set; }
    bool IsLoadedGame { get; }
    IReadOnlyList<string> TunnelNames { get; }
    int SelectedTunnelIndex { get; set; }

    IAsyncRelayCommand CreateGameCommand { get; }
    IAsyncRelayCommand CreateLoadedGameCommand { get; }
    IRelayCommand OpenAdvancedOptionsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
