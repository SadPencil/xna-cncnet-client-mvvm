using System;
using DXMainClientViewModel;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IGameProcessService for the View layer.
/// No game process support in Avalonia view-only mode.
/// </summary>
public class StubGameProcessService : IGameProcessService
{
    public event Action? GameProcessStarted;
    public event Action? GameProcessStarting;
    public event Action? GameProcessExited;
    public event Action? SetGraphicsModeRequested;

    public bool UseQres { get; set; }
    public bool SingleCoreAffinity { get; set; }
    public double PowerSavingFps => 0;
    public bool SavedIsFixedTimeStep => true;

    public void StartGameProcess() { }
}
