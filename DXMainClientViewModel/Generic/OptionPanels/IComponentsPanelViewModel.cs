#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IComponentsPanelViewModel
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
