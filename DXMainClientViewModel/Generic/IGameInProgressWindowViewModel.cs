using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface IGameInProgressWindowViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    bool IsGameRunning { get; }
    bool CanTerminateGame { get; }

    IRelayCommand ReturnToGameCommand { get; }
    IRelayCommand TerminateGameCommand { get; }
    IRelayCommand CancelCommand { get; }
}
