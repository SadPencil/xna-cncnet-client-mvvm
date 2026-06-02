using System;

namespace AvMainClientViewModel;

/// <summary>
/// Service interface for monitoring game host inactivity.
/// </summary>
public interface IGameHostInactiveCheckerService
{
    event EventHandler CloseEvent;
    event EventHandler WarningRequested;

    void Start();
    void Stop();
    void Reset();
}
