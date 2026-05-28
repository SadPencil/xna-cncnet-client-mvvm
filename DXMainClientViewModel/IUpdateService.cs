using System;

namespace DXMainClientViewModel
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
        /// Raised when local file versions have been checked.
        /// </summary>
        event Action OnLocalFileVersionsChecked;

        /// <summary>
        /// Checks local file versions.
        /// </summary>
        void CheckLocalFileVersions();
    }
}
