using System.ComponentModel;

namespace DXMainClientViewModel.Generic;

public interface IGameInProgressWindowViewModel : INotifyPropertyChanged
{
    bool IsGameInProgress { get; }
    bool IsCursorVisible { get; }

    void Initialize(bool savedIsFixedTimeStep);
}
