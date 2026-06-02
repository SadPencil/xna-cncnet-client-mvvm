using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvMainClientMvvmContract.Generic.OptionPanels;

public interface IUpdaterOptionsPanelViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> UpdateServerNames { get; }
    int SelectedUpdateServerIndex { get; set; }
    bool CheckForUpdatesAutomatically { get; set; }
    bool IsForceUpdateEnabled { get; set; }

    // Confirmation dialog state
    bool IsConfirmationVisible { get; }
    string ConfirmationMessage { get; }

    IRelayCommand MoveServerUpCommand { get; }
    IRelayCommand MoveServerDownCommand { get; }
    IRelayCommand ForceUpdateCommand { get; }
    IRelayCommand LoadSettingsCommand { get; }
    IRelayCommand SaveSettingsCommand { get; }
    IRelayCommand ConfirmYesCommand { get; }
    IRelayCommand ConfirmNoCommand { get; }
}
