using System.ComponentModel;

namespace DXMainClientViewModel.Generic;

public interface IGameInProgressWindowViewModel : INotifyPropertyChanged
{
    bool IsGameInProgress { get; }
    bool IsCursorVisible { get; }
    WindowState WindowState { get; }
}

/// <summary>
/// Represents the state of a window.
/// </summary>
public enum WindowState
{
    Normal,
    Minimized,
    Maximized
}
