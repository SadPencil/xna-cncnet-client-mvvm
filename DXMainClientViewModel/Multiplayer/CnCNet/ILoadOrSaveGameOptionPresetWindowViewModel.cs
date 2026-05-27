#nullable enable
using System.ComponentModel;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

public interface ILoadOrSaveGameOptionPresetWindowViewModel : INotifyPropertyChanged
{
    bool IsLoadMode { get; }
    IReadOnlyList<string> PresetNames { get; }
    int SelectedPresetIndex { get; set; }
    string PresetName { get; set; }

    IRelayCommand ConfirmCommand { get; }
    IRelayCommand DeleteSelectedPresetCommand { get; }
    IRelayCommand CancelCommand { get; }
}
