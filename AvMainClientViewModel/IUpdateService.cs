using System;
using System.Collections.Generic;

namespace AvMainClientViewModel
{
    /// <summary>
    /// Service interface for the updater.
    /// Abstracts ClientUpdater.Updater.
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// The current game version.
        /// </summary>
        string GameVersion { get; }

        /// <summary>
        /// The server's game version.
        /// </summary>
        string ServerGameVersion { get; }

        /// <summary>
        /// The current version state.
        /// </summary>
        VersionState VersionState { get; }

        /// <summary>
        /// Whether a manual update is required.
        /// </summary>
        bool ManualUpdateRequired { get; }

        /// <summary>
        /// The URL for manual download.
        /// </summary>
        string ManualDownloadURL { get; }

        /// <summary>
        /// The update size in kilobytes.
        /// </summary>
        int UpdateSizeInKb { get; }

        /// <summary>
        /// Available update mirrors.
        /// </summary>
        IReadOnlyList<string> UpdateMirrors { get; }

        /// <summary>
        /// Raised when local file versions have been checked.
        /// </summary>
        event Action OnLocalFileVersionsChecked;

        /// <summary>
        /// Raised when file identifiers have been updated (update check result).
        /// </summary>
        event Action FileIdentifiersUpdated;

        /// <summary>
        /// Raised when custom components are out of date.
        /// </summary>
        event Action OnCustomComponentsOutdated;

        /// <summary>
        /// Raised when the client needs to restart for an update.
        /// </summary>
        event EventHandler Restart;

        /// <summary>
        /// Raised when an update completes successfully.
        /// </summary>
        event EventHandler UpdateCompleted;

        /// <summary>
        /// Raised when an update is cancelled.
        /// </summary>
        event EventHandler UpdateCancelled;

        /// <summary>
        /// Raised when an update fails.
        /// </summary>
        event EventHandler<UpdateFailureEventArgs> UpdateFailed;

        /// <summary>
        /// Raised when update progress changes (file name, file percentage, total percentage).
        /// </summary>
        event Action<string, int, int> UpdateProgressChanged;

        /// <summary>
        /// Raised when local file check progress changes (checked count, total count).
        /// </summary>
        event Action<int, int> LocalFileCheckProgressChanged;

        /// <summary>
        /// Raised when a file download completes (archive name).
        /// </summary>
        event Action<string> FileDownloadCompleted;

        /// <summary>
        /// Checks local file versions.
        /// </summary>
        void CheckLocalFileVersions();

        /// <summary>
        /// Checks for available updates.
        /// </summary>
        void CheckForUpdates();

        /// <summary>
        /// Starts the update process.
        /// </summary>
        void StartUpdate();

        /// <summary>
        /// Stops the update process.
        /// </summary>
        void StopUpdate();

        /// <summary>
        /// Forces an update.
        /// </summary>
        void ForceUpdate();
    }

    /// <summary>
    /// Represents the version state of the client.
    /// </summary>
    public enum VersionState
    {
        UPTODATE,
        MISMATCHED,
        UNKNOWN,
        UPDATEINPROGRESS,
        UPDATECHECKINPROGRESS,
        OUTDATED
    }

    /// <summary>
    /// Event args for update failure.
    /// </summary>
    public class UpdateFailureEventArgs : EventArgs
    {
        public string Reason { get; }

        public UpdateFailureEventArgs(string reason)
        {
            Reason = reason;
        }
    }
}
