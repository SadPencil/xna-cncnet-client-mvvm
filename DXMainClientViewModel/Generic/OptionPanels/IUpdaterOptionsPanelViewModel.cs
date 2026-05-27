using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic.OptionPanels;

public interface IUpdaterOptionsPanelViewModel : INotifyPropertyChanged
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
