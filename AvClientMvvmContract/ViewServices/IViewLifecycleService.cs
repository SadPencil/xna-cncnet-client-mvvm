using System;

namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Service that notifies ViewModels about application lifecycle events.
/// </summary>
public interface IViewLifecycleService
{
    event EventHandler Closing;

    /// <summary>
    /// Requests the application to shut down.
    /// </summary>
    void Shutdown();
}
