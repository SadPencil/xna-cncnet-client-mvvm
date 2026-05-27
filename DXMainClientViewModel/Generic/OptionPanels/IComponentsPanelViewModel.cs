using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic.OptionPanels;

public interface IComponentsPanelViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> ComponentNames { get; }
    IReadOnlyList<string> ComponentActionTexts { get; }
    IReadOnlyList<string> ComponentStatusTexts { get; }
    int SelectedComponentIndex { get; set; }
    bool IsBusy { get; }

    IAsyncRelayCommand InstallSelectedComponentCommand { get; }
    IAsyncRelayCommand UpdateSelectedComponentCommand { get; }
    IAsyncRelayCommand UninstallSelectedComponentCommand { get; }
    IAsyncRelayCommand CancelDownloadsCommand { get; }
    IRelayCommand RefreshComponentsCommand { get; }

    void Initialize();
}
