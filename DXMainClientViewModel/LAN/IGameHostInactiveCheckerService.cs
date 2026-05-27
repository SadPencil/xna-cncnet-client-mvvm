namespace DXMainClientViewModel.LAN;

public interface IGameHostInactiveCheckerService
{
    bool IsMonitoring { get; }
    int WarningTimeoutSeconds { get; }
    int DisconnectTimeoutSeconds { get; }
    bool HasWarningThresholdElapsed { get; }
    bool HasDisconnectThresholdElapsed { get; }

    void Start();
    void Stop();
    void ResetActivity();
}
