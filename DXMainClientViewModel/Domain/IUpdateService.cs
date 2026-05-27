using System.Threading.Tasks;

namespace DXMainClientViewModel.Domain;

public interface IUpdateService
{
    bool IsCheckingForUpdates { get; }
    bool IsUpdateAvailable { get; }
    bool IsDownloadingUpdate { get; }
    string AvailableVersion { get; }
    int DownloadProgressPercentage { get; }

    Task CheckForUpdatesAsync();
    Task StartUpdateAsync();
    void CancelUpdate();
    void OpenManualDownloadPage();
}
