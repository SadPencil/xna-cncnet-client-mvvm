#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IGameInProgressWindowViewModel
{
    string StatusText { get; }
    bool IsGameRunning { get; }
    bool CanTerminateGame { get; }

    IRelayCommand ReturnToGameCommand { get; }
    IRelayCommand TerminateGameCommand { get; }
    IRelayCommand CancelCommand { get; }
}
