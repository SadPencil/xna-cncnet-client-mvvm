namespace AvClientMvvmContract.ViewServices;

/// <summary>
/// Manages the client application process lifecycle.
/// Launches a new instance and terminates the current one.
/// </summary>
public interface IProcessLifecycleService
{
    /// <summary>
    /// Launches a new client instance, then terminates the current one.
    /// </summary>
    void Restart();

    /// <summary>
    /// Launches a new client instance with administrator privileges (Windows only),
    /// then terminates the current one.
    /// </summary>
    void RestartAsAdmin();

    /// <summary>
    /// Terminates the client application.
    /// </summary>
    void Shutdown();
}
