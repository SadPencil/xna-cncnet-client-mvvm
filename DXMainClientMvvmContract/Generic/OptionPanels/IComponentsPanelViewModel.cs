using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic.OptionPanels;

public interface IComponentsPanelViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> ComponentNames { get; }
    IReadOnlyList<string> ComponentActionTexts { get; }
    IReadOnlyList<string> ComponentStatusTexts { get; }
    int SelectedComponentIndex { get; set; }
    bool IsBusy { get; }

    // Confirmation dialog state
    bool IsConfirmationVisible { get; }
    string ConfirmationMessage { get; }

    // Message box state
    bool IsMessageBoxVisible { get; }
    string MessageBoxTitle { get; }
    string MessageBoxMessage { get; }

    IAsyncRelayCommand InstallSelectedComponentCommand { get; }
    IAsyncRelayCommand UpdateSelectedComponentCommand { get; }
    IAsyncRelayCommand UninstallSelectedComponentCommand { get; }
    IAsyncRelayCommand CancelDownloadsCommand { get; }
    IRelayCommand RefreshComponentsCommand { get; }
    IRelayCommand ConfirmYesCommand { get; }
    IRelayCommand ConfirmNoCommand { get; }
    IRelayCommand DismissMessageBoxCommand { get; }
}
