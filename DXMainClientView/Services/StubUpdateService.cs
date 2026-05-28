using System;
using System.Collections.Generic;
using DXMainClientViewModel;

namespace DXMainClientView.Services;

/// <summary>
/// Stub implementation of IUpdateService for the Avalonia view host.
/// Provides minimal functionality so the LoadingScreenViewModel can be instantiated.
/// </summary>
public class StubUpdateService : IUpdateService
{
    public string GameVersion => "1.0.0";
    public string ServerGameVersion => "1.0.0";
    public VersionState VersionState => VersionState.UPTODATE;
    public bool ManualUpdateRequired => false;
    public string ManualDownloadURL => string.Empty;
    public int UpdateSizeInKb => 0;
    public IReadOnlyList<string> UpdateMirrors => Array.Empty<string>();

    public event Action? OnLocalFileVersionsChecked;
    public event Action? FileIdentifiersUpdated;
    public event Action? OnCustomComponentsOutdated;
    public event EventHandler? Restart;
    public event EventHandler? UpdateCompleted;
    public event EventHandler? UpdateCancelled;
    public event EventHandler<UpdateFailureEventArgs>? UpdateFailed;
    public event Action<string, int, int>? UpdateProgressChanged;
    public event Action<int, int>? LocalFileCheckProgressChanged;
    public event Action<string>? FileDownloadCompleted;

    public void CheckLocalFileVersions()
    {
        OnLocalFileVersionsChecked?.Invoke();
    }

    public void CheckForUpdates() { }
    public void StartUpdate() { }
    public void StopUpdate() { }
    public void ForceUpdate() { }
}
