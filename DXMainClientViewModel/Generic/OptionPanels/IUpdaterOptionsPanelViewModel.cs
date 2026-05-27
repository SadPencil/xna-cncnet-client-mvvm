#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IUpdaterOptionsPanelViewModel
{
    IReadOnlyList<string> UpdateServerNames { get; }
    int SelectedUpdateServerIndex { get; set; }
    bool CheckForUpdatesAutomatically { get; set; }

    IRelayCommand MoveServerUpCommand { get; }
    IRelayCommand MoveServerDownCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
}
