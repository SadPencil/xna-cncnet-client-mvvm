using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer;

public interface IGameInformationPanelViewModel : INotifyPropertyChanged
{
    string SelectedGameName { get; }
    string HostName { get; }
    string MapName { get; }
    string GameModeName { get; }
    int PlayerCount { get; }
    int MaxPlayers { get; }
    int Ping { get; }
    string GameVersion { get; }
    int SkillLevelIndex { get; }
    string SkillLevelName { get; }
    bool IsLocked { get; }
    bool IsPasswordProtected { get; }
    bool IsCompatible { get; }
    bool HasGameInfo { get; }
    string? MapHash { get; }
    IReadOnlyList<string> PlayerNames { get; }

    IRelayCommand RefreshCommand { get; }
    IRelayCommand ClearSelectionCommand { get; }
}
