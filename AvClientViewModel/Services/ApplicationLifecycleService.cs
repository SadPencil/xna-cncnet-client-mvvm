using System;

namespace AvClientViewModel.Services;

/// <summary>
/// Service that notifies ViewModels about application lifecycle events.
/// Registered as a singleton by the composition root.
/// </summary>
public interface IApplicationLifecycleService
{
    event EventHandler ApplicationClosing;

    /// <summary>
    /// Requests the application to shut down gracefully.
    /// Fires ApplicationClosing so subscribers can clean up, then exits.
    /// </summary>
    void Shutdown();
}

public class ApplicationLifecycleService : IApplicationLifecycleService
{
    private bool _isShuttingDown;

    public event EventHandler? ApplicationClosing;

    public void Shutdown()
    {
        if (_isShuttingDown)
            return;

        _isShuttingDown = true;
        ApplicationClosing?.Invoke(this, EventArgs.Empty);
        Environment.Exit(0);
    }
}
