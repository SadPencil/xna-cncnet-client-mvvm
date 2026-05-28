using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IOptionsWindowViewModel : INotifyPropertyChanged
{
    int SelectedPanelIndex { get; set; }
    bool IsComponentsPanelVisible { get; }
    bool IsComponentDownloadInProgress { get; }
    bool IsVisible { get; set; }

    IRelayCommand SaveCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand OpenComponentsPanelCommand { get; }

    void Initialize();
}
