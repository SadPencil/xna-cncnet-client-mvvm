using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowViewModel : INotifyPropertyChanged
{
    bool IsLoadMode { get; }
    IReadOnlyList<string> PresetNames { get; }
    int SelectedPresetIndex { get; set; }
    string PresetName { get; set; }
    bool IsWindowVisible { get; set; }
    bool IsNewPresetNameEnabled { get; }
    bool IsConfirmEnabled { get; }
    bool IsDeleteEnabled { get; }

    IRelayCommand ConfirmCommand { get; }
    IRelayCommand DeleteSelectedPresetCommand { get; }
    IRelayCommand CancelCommand { get; }
}
