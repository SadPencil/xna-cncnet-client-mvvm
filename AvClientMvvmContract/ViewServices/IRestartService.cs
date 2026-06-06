using System;

namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Service for restarting the client, with optional admin elevation.
/// </summary>
public interface IRestartService
{
    /// <summary>
    /// Restarts the client normally.
    /// </summary>
    void RestartClient();

    /// <summary>
    /// Restarts the client with administrator privileges (Windows only).
    /// </summary>
    void RestartAsAdmin();

    /// <summary>
    /// Requests the application to shut down after a restart has been initiated.
    /// </summary>
    void Shutdown();
}
