// checked
using CommunityToolkit.Mvvm.ComponentModel;
using ClientCore;
using ClientCore.Enums;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the game-in-progress window.
    /// Handles debug log management, screenshot processing, and game process lifecycle.
    /// </summary>
    public partial class GameInProgressWindowViewModel : ObservableObject, IGameInProgressWindowViewModel
    {
        private const double POWER_SAVING_FPS = 5.0;

        private readonly IGameProcessService gameProcessService;

        private bool nativeCursorUsed = false;
        private bool savedIsFixedTimeStep;

        private List<string> debugSnapshotDirectories;
        private DateTime debugLogLastWriteTime;
        private bool deletingLogFilesFailed = false;

        [ObservableProperty]
        private bool isGameInProgress;

        [ObservableProperty]
        private bool isCursorVisible = true;

        /// <summary>
        /// Raised when the View should set the graphics mode (e.g., after game exit with borderless windowed).
        /// </summary>
        public event Action SetGraphicsModeRequested;

        /// <summary>
        /// Raised when the View should minimize the window.
        /// </summary>
        public event Action MinimizeWindowRequested;

        /// <summary>
        /// Raised when the View should maximize the window.
        /// </summary>
        public event Action MaximizeWindowRequested;

        public GameInProgressWindowViewModel(IGameProcessService gameProcessService)
        {
            this.gameProcessService = gameProcessService;
        }

        public void Initialize(bool savedIsFixedTimeStep)
        {
            this.savedIsFixedTimeStep = savedIsFixedTimeStep;

            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                try
                {
                    FileInfo debugLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "debug", "debug.log");

                    if (debugLogFileInfo.Exists)
                        debugLogLastWriteTime = debugLogFileInfo.LastWriteTime;
                }
                catch { }
            }

            gameProcessService.GameProcessStarted += OnGameProcessStarted;
            gameProcessService.GameProcessExited += OnGameProcessExited;
        }

        private void OnGameProcessStarted()
        {
            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                debugSnapshotDirectories = GetAllDebugSnapshotDirectories();
            }
            else
            {
                try
                {
                    SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "EXCEPT.TXT");

                    for (int i = 0; i < 8; i++)
                        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "SYNC" + i + ".TXT");

                    deletingLogFilesFailed = false;
                }
                catch (Exception ex)
                {
                    Logger.Log("Exception when deleting error log files! Message: " + ex.ToString());
                    deletingLogFilesFailed = true;
                }
            }

            IsGameInProgress = true;
            IsCursorVisible = false;
            ProgramConstants.IsInGame = true;

#if WINFORMS
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                MinimizeWindowRequested?.Invoke();
#endif
        }

        private void OnGameProcessExited()
        {
            IsGameInProgress = false;
            IsCursorVisible = true;
            ProgramConstants.IsInGame = false;

#if WINFORMS
            if (UserINISettings.Instance.MinimizeWindowsOnGameStart)
                MaximizeWindowRequested?.Invoke();
#endif
            UserINISettings.Instance.ReloadSettings();

            if (UserINISettings.Instance.BorderlessWindowedClient)
            {
                SetGraphicsModeRequested?.Invoke();
            }

            DateTime dtn = DateTime.Now;

            if (ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
            {
                Task.Run(ProcessScreenshots);

                string snapshotDirectory = GetNewestDebugSnapshotDirectory();
                bool snapshotCreated = snapshotDirectory != null;

                snapshotDirectory = snapshotDirectory ?? SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "debug", FormattableString.Invariant($"snapshot-{dtn.ToString("yyyyMMdd-HHmmss")}"));

                bool debugLogModified = false;
                FileInfo debugLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, "debug", "debug.log");
                DateTime lastWriteTime = new DateTime();

                if (debugLogFileInfo.Exists)
                    lastWriteTime = debugLogFileInfo.LastAccessTime;

                if (!lastWriteTime.Equals(debugLogLastWriteTime))
                {
                    debugLogModified = true;
                    debugLogLastWriteTime = lastWriteTime;
                }

                if (CopySyncErrorLogs(snapshotDirectory, null) || snapshotCreated)
                {
                    FileInfo snapShotDebugLogFileInfo = SafePath.GetFile(snapshotDirectory, "debug.log");

                    if (debugLogFileInfo.Exists && !snapShotDebugLogFileInfo.Exists && debugLogModified)
                        File.Copy(debugLogFileInfo.FullName, snapShotDebugLogFileInfo.FullName);

                    CopyErrorLog(snapshotDirectory, "syringe.log", null);
                }
            }
            else
            {
                if (deletingLogFilesFailed)
                    return;

                CopyErrorLog(SafePath.CombineDirectoryPath(ProgramConstants.ClientUserFilesPath, "GameCrashLogs"), "EXCEPT.TXT", dtn);
                CopySyncErrorLogs(SafePath.CombineDirectoryPath(ProgramConstants.ClientUserFilesPath, "SyncErrorLogs"), dtn);
            }
        }

        /// <summary>
        /// Returns the power-saving FPS value.
        /// </summary>
        public double GetPowerSavingFps() => POWER_SAVING_FPS;

        /// <summary>
        /// Returns the saved IsFixedTimeStep value from before the game started.
        /// </summary>
        public bool GetSavedIsFixedTimeStep() => savedIsFixedTimeStep;

        /// <summary>
        /// Attempts to copy a general error log from game directory to another directory.
        /// </summary>
        private bool CopyErrorLog(string directory, string filename, DateTime? dateTime)
        {
            bool copied = false;

            try
            {
                FileInfo errorLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, filename);

                if (errorLogFileInfo.Exists)
                {
                    DirectoryInfo errorLogDirectoryInfo = SafePath.GetDirectory(directory);

                    if (!errorLogDirectoryInfo.Exists)
                        errorLogDirectoryInfo.Create();

                    Logger.Log("The game crashed! Copying " + filename + " file.");

                    string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                    string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                        timeStamp + Path.GetExtension(filename);

                    File.Copy(errorLogFileInfo.FullName, SafePath.CombineFilePath(directory, filenameCopy));
                    copied = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for " + filename + " file. Message: " + ex.ToString());
            }
            return copied;
        }

        /// <summary>
        /// Attempts to copy sync error logs from game directory to another directory.
        /// </summary>
        private bool CopySyncErrorLogs(string directory, DateTime? dateTime)
        {
            bool copied = false;

            try
            {
                for (int i = 0; i < 8; i++)
                {
                    string filename = "SYNC" + i + ".TXT";
                    FileInfo syncErrorLogFileInfo = SafePath.GetFile(ProgramConstants.GamePath, filename);

                    if (syncErrorLogFileInfo.Exists)
                    {
                        DirectoryInfo syncErrorLogDirectoryInfo = SafePath.GetDirectory(directory);

                        if (!syncErrorLogDirectoryInfo.Exists)
                            syncErrorLogDirectoryInfo.Create();

                        Logger.Log("There was a sync error! Copying file " + filename);

                        string timeStamp = dateTime.HasValue ? dateTime.Value.ToString("_yyyy_MM_dd_HH_mm") : "";

                        string filenameCopy = Path.GetFileNameWithoutExtension(filename) +
                            timeStamp + Path.GetExtension(filename);

                        File.Copy(syncErrorLogFileInfo.FullName, SafePath.CombineFilePath(directory, filenameCopy));
                        copied = true;
                        syncErrorLogFileInfo.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while checking for SYNCX.TXT files. Message: " + ex.ToString());
            }
            return copied;
        }

        /// <summary>
        /// Returns the first debug snapshot directory found in Ares debug log directory that was created after last game launch and isn't empty.
        /// </summary>
        private string GetNewestDebugSnapshotDirectory()
        {
            string snapshotDirectory = null;

            if (debugSnapshotDirectories != null)
            {
                var newDirectories = GetAllDebugSnapshotDirectories().Except(debugSnapshotDirectories);

                foreach (string directory in newDirectories)
                {
                    if (Directory.EnumerateFileSystemEntries(directory).Any())
                        snapshotDirectory = directory;
                    else
                    {
                        try
                        {
                            Directory.Delete(directory);
                        }
                        catch { }
                    }
                }
            }

            return snapshotDirectory;
        }

        /// <summary>
        /// Returns list of all debug snapshot directories in Ares debug logs directory.
        /// </summary>
        private List<string> GetAllDebugSnapshotDirectories()
        {
            var directories = new List<string>();

            try
            {
                directories.AddRange(Directory.GetDirectories(SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "debug"), "snapshot-*"));
            }
            catch { }

            return directories;
        }

        /// <summary>
        /// Converts BMP screenshots to PNG and copies them from game directory to Screenshots sub-directory.
        /// </summary>
        private void ProcessScreenshots()
        {
            IEnumerable<FileInfo> files = SafePath.GetDirectory(ProgramConstants.GamePath).EnumerateFiles("SCRN*.bmp");
            DirectoryInfo screenshotsDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, "Screenshots");

            if (!screenshotsDirectory.Exists)
            {
                try
                {
                    screenshotsDirectory.Create();
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: An error occured trying to create Screenshots directory. Message: " + ex.ToString());
                    return;
                }
            }

            foreach (FileInfo file in files)
            {
                try
                {
                    using FileStream stream = file.OpenRead();
                    using var image = Image.Load(stream);
                    FileInfo newFile = SafePath.GetFile(screenshotsDirectory.FullName, FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(file.FullName)}.png"));
                    using FileStream newFileStream = newFile.OpenWrite();

                    image.SaveAsPng(newFileStream);
                }
                catch (Exception ex)
                {
                    Logger.Log("ProcessScreenshots: Error occured when trying to save " + Path.GetFileNameWithoutExtension(file.FullName) + ".png. Message: " + ex.ToString());
                    continue;
                }

                Logger.Log("ProcessScreenshots: " + Path.GetFileNameWithoutExtension(file.FullName) + ".png has been saved to Screenshots directory.");
                file.Delete();
            }
        }
    }
}
