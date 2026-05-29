using System;
using System.Collections.Generic;
using System.Linq;

namespace DXMainClientViewModel
{
    /// <summary>
    /// Adapter wrapping ClientUpdater.Updater static class into IUpdateService.
    /// </summary>
    public class ClientUpdateService : IUpdateService
    {
        public string GameVersion => ClientUpdater.Updater.GameVersion;

        public string ServerGameVersion => ClientUpdater.Updater.ServerGameVersion;

        public VersionState VersionState => (VersionState)(int)ClientUpdater.Updater.VersionState;

        public bool ManualUpdateRequired => ClientUpdater.Updater.ManualUpdateRequired;

        public string ManualDownloadURL => ClientUpdater.Updater.ManualDownloadURL;

        public int UpdateSizeInKb => ClientUpdater.Updater.UpdateSizeInKb;

        public IReadOnlyList<string> UpdateMirrors =>
            ClientUpdater.Updater.UpdateMirrors?.Select(m => m.Name).ToList()
            ?? (IReadOnlyList<string>)Array.Empty<string>();

        // Updater uses custom delegate types, so we wrap with private fields
        // and forward to the IUpdateService events.

        private Action onLocalFileVersionsChecked;
        private Action fileIdentifiersUpdated;
        private Action onCustomComponentsOutdated;
        private EventHandler restart;
        private EventHandler updateCompleted;
        private EventHandler<UpdateFailureEventArgs> updateFailed;
        private Action<string, int, int> updateProgressChanged;
        private Action<int, int> localFileCheckProgressChanged;
        private Action<string> fileDownloadCompleted;

        public event Action OnLocalFileVersionsChecked
        {
            add => onLocalFileVersionsChecked += value;
            remove => onLocalFileVersionsChecked -= value;
        }

        public event Action FileIdentifiersUpdated
        {
            add => fileIdentifiersUpdated += value;
            remove => fileIdentifiersUpdated -= value;
        }

        public event Action OnCustomComponentsOutdated
        {
            add => onCustomComponentsOutdated += value;
            remove => onCustomComponentsOutdated -= value;
        }

        public event EventHandler Restart
        {
            add => restart += value;
            remove => restart -= value;
        }

        public event EventHandler UpdateCompleted
        {
            add => updateCompleted += value;
            remove => updateCompleted -= value;
        }

        public event EventHandler UpdateCancelled;

        public event EventHandler<UpdateFailureEventArgs> UpdateFailed
        {
            add => updateFailed += value;
            remove => updateFailed -= value;
        }

        public event Action<string, int, int> UpdateProgressChanged
        {
            add => updateProgressChanged += value;
            remove => updateProgressChanged -= value;
        }

        public event Action<int, int> LocalFileCheckProgressChanged
        {
            add => localFileCheckProgressChanged += value;
            remove => localFileCheckProgressChanged -= value;
        }

        public event Action<string> FileDownloadCompleted
        {
            add => fileDownloadCompleted += value;
            remove => fileDownloadCompleted -= value;
        }

        public ClientUpdateService()
        {
            ClientUpdater.Updater.OnLocalFileVersionsChecked += () => onLocalFileVersionsChecked?.Invoke();
            ClientUpdater.Updater.FileIdentifiersUpdated += () => fileIdentifiersUpdated?.Invoke();
            ClientUpdater.Updater.OnCustomComponentsOutdated += () => onCustomComponentsOutdated?.Invoke();
            ClientUpdater.Updater.Restart += (s, e) => restart?.Invoke(s, e);
            ClientUpdater.Updater.OnUpdateCompleted += () => updateCompleted?.Invoke(this, EventArgs.Empty);
            ClientUpdater.Updater.OnUpdateFailed += ex =>
                updateFailed?.Invoke(this, new UpdateFailureEventArgs(ex.Message));
            ClientUpdater.Updater.UpdateProgressChanged += (f, p, t) =>
                updateProgressChanged?.Invoke(f, p, t);
            ClientUpdater.Updater.LocalFileCheckProgressChanged += (c, total) =>
                localFileCheckProgressChanged?.Invoke(c, total);
            ClientUpdater.Updater.OnFileDownloadCompleted += name =>
                fileDownloadCompleted?.Invoke(name);
        }

        public void CheckLocalFileVersions() => ClientUpdater.Updater.CheckLocalFileVersions();

        public void CheckForUpdates() => ClientUpdater.Updater.CheckForUpdates();

        public void StartUpdate() => ClientUpdater.Updater.StartUpdate();

        public void StopUpdate() => ClientUpdater.Updater.StopUpdate();

        public void ForceUpdate()
        {
            ClientUpdater.Updater.ClearVersionInfo();
            ClientUpdater.Updater.CheckForUpdates();
        }
    }
}
