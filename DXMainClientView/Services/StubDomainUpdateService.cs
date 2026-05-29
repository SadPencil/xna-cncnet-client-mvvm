using System.Threading.Tasks;
using DXMainClientViewModel.Domain;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of Domain.IUpdateService for the View layer.
/// </summary>
public class StubDomainUpdateService : IUpdateService
{
    public bool IsCheckingForUpdates => false;
    public bool IsUpdateAvailable => false;
    public bool IsDownloadingUpdate => false;
    public string AvailableVersion => "1.0.0";
    public int DownloadProgressPercentage => 0;

    public Task CheckForUpdatesAsync() => Task.CompletedTask;
    public Task StartUpdateAsync() => Task.CompletedTask;
    public void CancelUpdate() { }
    public void OpenManualDownloadPage() { }
}
