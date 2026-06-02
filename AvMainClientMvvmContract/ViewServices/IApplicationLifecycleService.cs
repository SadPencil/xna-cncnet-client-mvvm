using System;

namespace AvMainClientMvvmContract.ViewServices;

/// <summary>
/// Service that notifies ViewModels about application lifecycle events.
/// </summary>
public interface IApplicationLifecycleService
{
    event EventHandler ApplicationClosing;

    /// <summary>
    /// Requests the application to shut down.
    /// </summary>
    void Shutdown();
}
