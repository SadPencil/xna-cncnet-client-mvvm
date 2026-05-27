using System.Collections.Generic;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IGameLoadingWindowViewModel : INotifyPropertyChanged
{
    IReadOnlyList<string> SavedGameNames { get; }
    int SelectedSavedGameIndex { get; set; }
    bool CanLoadGame { get; }
    bool CanDeleteSavedGame { get; }

    IRelayCommand LoadGameCommand { get; }
    IRelayCommand DeleteSavedGameCommand { get; }
    IRelayCommand CancelCommand { get; }
    IRelayCommand RefreshCommand { get; }

    void Initialize();
}
