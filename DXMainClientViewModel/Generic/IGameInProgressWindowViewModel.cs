using System;
using System.ComponentModel;

namespace DXMainClientViewModel.Generic;

public interface IGameInProgressWindowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Whether a game is currently in progress.
    /// </summary>
    bool IsGameInProgress { get; }

    /// <summary>
    /// Whether the cursor should be visible.
    /// </summary>
    bool IsCursorVisible { get; }

    /// <summary>
    /// Raised when the View should set the graphics mode.
    /// </summary>
    event Action SetGraphicsModeRequested;

    /// <summary>
    /// Raised when the View should minimize the window.
    /// </summary>
    event Action MinimizeWindowRequested;

    /// <summary>
    /// Raised when the View should maximize the window.
    /// </summary>
    event Action MaximizeWindowRequested;

    /// <summary>
    /// Initializes the ViewModel.
    /// </summary>
    void Initialize(bool savedIsFixedTimeStep);

    /// <summary>
    /// Returns the power-saving FPS value.
    /// </summary>
    double GetPowerSavingFps();

    /// <summary>
    /// Returns the saved IsFixedTimeStep value from before the game started.
    /// </summary>
    bool GetSavedIsFixedTimeStep();
}
