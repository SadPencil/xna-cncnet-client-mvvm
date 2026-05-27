#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ILoadOrSaveGameOptionPresetWindowViewModel
{
    bool IsLoadMode { get; }
    IReadOnlyList<string> PresetNames { get; }
    int SelectedPresetIndex { get; set; }
    string PresetName { get; set; }

    IRelayCommand ConfirmCommand { get; }
    IRelayCommand DeleteSelectedPresetCommand { get; }
    IRelayCommand CancelCommand { get; }
}
