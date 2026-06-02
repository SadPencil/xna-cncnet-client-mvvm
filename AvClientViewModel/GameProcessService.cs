using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using ClientCore;
using ClientCore.INIProcessing;

using Rampastring.Tools;

namespace AvClientViewModel
{
    /// <summary>
    /// Implementation of IGameProcessService.
    /// Handles launching the game executable with QRes/single-core affinity support.
    /// Adapted from ClientGUI.GameProcessLogic.
    /// </summary>
    public class GameProcessService : IGameProcessService
    {
        public event Action GameProcessStarted;
        public event Action GameProcessStarting;
        public event Action GameProcessExited;
        public event Action SetGraphicsModeRequested;

        public bool UseQres { get; set; }
        public bool SingleCoreAffinity { get; set; }
        public double PowerSavingFps { get; set; }
        public bool SavedIsFixedTimeStep { get; set; }

        public void StartGameProcess()
        {
            Logger.Log("About to launch main game executable.");

            int waitTimes = 0;
            while (PreprocessorBackgroundTask.Instance.IsRunning)
            {
                Logger.Log("The preprocessor background task is still running. Wait for it...");
                Thread.Sleep(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    Logger.Log("INI preprocessing not complete. Aborting game launch.");
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string gameExecutableName;
            string additionalExecutableName = string.Empty;

            if (osVersion == OSVersion.UNIX)
                gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
            else
            {
                string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
                if (string.IsNullOrEmpty(launcherExecutableName))
                    gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
                else
                {
                    gameExecutableName = launcherExecutableName;
                    additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
                }
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

            GameProcessStarting?.Invoke();

            if (UserINISettings.Instance.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                var qresProcess = new Process();
                qresProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
                qresProcess.StartInfo.UseShellExecute = false;

                if (!string.IsNullOrEmpty(extraCommandLine))
                    qresProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    qresProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";
                qresProcess.EnableRaisingEvents = true;
                qresProcess.Exited += Process_Exited;
                Logger.Log("Launch executable: " + qresProcess.StartInfo.FileName);
                Logger.Log("Launch arguments: " + qresProcess.StartInfo.Arguments);
                try
                {
                    qresProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching QRes: " + ex.ToString());
                    Process_Exited(qresProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    qresProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                string arguments;

                if (!string.IsNullOrWhiteSpace(extraCommandLine))
                    arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    arguments = additionalExecutableName + "-SPAWN";

                FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, gameExecutableName);

                var gameProcess = new Process();
                gameProcess.StartInfo.FileName = gameFileInfo.FullName;
                gameProcess.StartInfo.Arguments = arguments;
                gameProcess.StartInfo.UseShellExecute = false;

                gameProcess.EnableRaisingEvents = true;
                gameProcess.Exited += Process_Exited;

                Logger.Log("Launch executable: " + gameProcess.StartInfo.FileName);
                Logger.Log("Launch arguments: " + gameProcess.StartInfo.Arguments);
                try
                {
                    gameProcess.Start();
                    Logger.Log("GameProcessService: Process started.");
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameFileInfo.Name + ": " + ex.ToString());
                    Process_Exited(gameProcess, EventArgs.Empty);
                    return;
                }

                if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    && Environment.ProcessorCount > 1 && SingleCoreAffinity)
                {
                    gameProcess.ProcessorAffinity = (IntPtr)2;
                }
            }

            GameProcessStarted?.Invoke();
            Logger.Log("Waiting for qres.dat or game executable to exit.");
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            Logger.Log("GameProcessService: Process exited.");
            if (sender is Process proc)
            {
                proc.Exited -= Process_Exited;
                proc.Dispose();
            }
            GameProcessExited?.Invoke();
        }
    }
}
