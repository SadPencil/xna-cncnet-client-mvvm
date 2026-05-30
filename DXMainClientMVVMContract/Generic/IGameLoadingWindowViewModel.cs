using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMVVMContract.Generic;

public interface IGameLoadingWindowViewModel : INotifyPropertyChanged
{
    List<string> SavedGameNames { get; }
    int SelectedSavedGameIndex { get; set; }
    bool CanLoadGame { get; }
    bool CanDeleteSavedGame { get; }
    bool IsVisible { get; set; }
    bool ShowDeleteConfirmation { get; set; }
    string DeleteConfirmationMessage { get; }

    IRelayCommand LoadGameCommand { get; }
    IRelayCommand DeleteSavedGameCommand { get; }
    IRelayCommand ConfirmDeleteCommand { get; }
    IRelayCommand CancelDeleteCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }
}
