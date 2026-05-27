#nullable enable
using System.ComponentModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameLobbySettingsWindowViewModel : INotifyPropertyChanged
{
    string GameName { get; set; }
    string Password { get; set; }
    int MaxPlayers { get; set; }
    IReadOnlyList<string> SkillLevelNames { get; }
    int SelectedSkillLevelIndex { get; set; }

    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
