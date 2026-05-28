using System.ComponentModel;

namespace DXMainClientViewModel.Generic;

public interface IGameInProgressWindowViewModel : INotifyPropertyChanged
{
    bool IsGameInProgress { get; }
    bool IsCursorVisible { get; }
    bool ShouldMinimizeWindow { get; set; }
    bool ShouldMaximizeWindow { get; set; }
}
