using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Rampastring.Tools;
using Serilog;

namespace ClientCore.INIProcessing
{
    /// <summary>
    /// Background task for pre-processing INI files.
    /// Singleton.
    /// </summary>
    public class PreprocessorBackgroundTask
    {
        private PreprocessorBackgroundTask()
        {
        }

        private static PreprocessorBackgroundTask _instance;
        public static PreprocessorBackgroundTask Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PreprocessorBackgroundTask();

                return _instance;
            }
        }

        private Task task;

        public bool IsRunning => !task.IsCompleted;

        public void Run()
        {
            task = Task.Run(CheckFiles);
        }

        private static void CheckFiles()
        {
            Log.Information("Starting background processing of INI files.");

            DirectoryInfo iniFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "INI", "Base");

            if (!iniFolder.Exists)
            {
                Log.Information("/INI/Base does not exist, skipping background processing of INI files.");
                return;
            }

            IniPreprocessInfoStore infoStore = new IniPreprocessInfoStore();
            infoStore.Load();

            IniPreprocessor processor = new IniPreprocessor();

            IEnumerable<FileInfo> iniFiles = iniFolder.EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly);

            int processedCount = 0;

            foreach (FileInfo iniFile in iniFiles)
            {
                if (iniFile.Name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!infoStore.IsIniUpToDate(iniFile.Name))
                {
                    Log.Information("INI file " + iniFile.Name + " is not processed or outdated, re-processing it.");

                    string sourcePath = iniFile.FullName;
                    string destinationPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", iniFile.Name);

                    processor.ProcessIni(sourcePath, destinationPath);

                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(destinationPath);
                    infoStore.UpsertRecord(iniFile.Name, sourceHash, destinationHash);
                    processedCount++;
                }
                else
                {
                    Log.Information("INI file " + iniFile.Name + " is up to date.");
                }
            }

            if (processedCount > 0)
            {
                Log.Information("Writing preprocessed INI info store.");
                infoStore.Write();
            }

            Log.Information("Ended background processing of INI files.");
        }
    }
}
