using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;
using System;
using System.Diagnostics;

namespace DXMainClientViewModel.Generic
{
    /// <summary>
    /// ViewModel for the extras menu window.
    /// Handles map editor launching and credits URL opening.
    /// </summary>
    public partial class ExtrasWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isStatisticsAvailable;

        [ObservableProperty]
        private bool isMapEditorAvailable;

        /// <summary>
        /// Raised when the statistics window should be shown.
        /// </summary>
        public event Action StatisticsRequested;

        /// <summary>
        /// Raised when the window should be closed.
        /// </summary>
        public event Action CloseRequested;

        public ExtrasWindowViewModel()
        {
            IsMapEditorAvailable = !string.IsNullOrEmpty(ClientConfiguration.Instance.MapEditorExePath);
        }

        [RelayCommand]
        private void OpenStatistics()
        {
            StatisticsRequested?.Invoke();
        }

        [RelayCommand]
        private void OpenMapEditor()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();
            using var mapEditorProcess = new Process();

            if (osVersion != OSVersion.UNIX)
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.MapEditorExePath);
            else
                mapEditorProcess.StartInfo.FileName = SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.UnixMapEditorExePath);

            mapEditorProcess.StartInfo.UseShellExecute = false;

            mapEditorProcess.Start();
        }

        [RelayCommand]
        private void OpenCredits()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.CreditsURL);
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke();
        }
    }
}
