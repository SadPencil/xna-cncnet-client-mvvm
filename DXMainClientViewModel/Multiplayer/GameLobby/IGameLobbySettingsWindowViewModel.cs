#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameLobbySettingsWindowViewModel
{
    string GameName { get; set; }
    string Password { get; set; }
    int MaxPlayers { get; set; }
    IReadOnlyList<string> SkillLevelNames { get; }
    int SelectedSkillLevelIndex { get; set; }

    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand CancelCommand { get; }
}
