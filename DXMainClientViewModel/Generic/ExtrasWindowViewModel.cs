using DXMainClientMVVMContract.Generic;

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
    public partial class ExtrasWindowViewModel : ObservableObject, IExtrasWindowViewModel
    {
        private readonly StatisticsWindowViewModel statisticsWindowViewModel;

        [ObservableProperty]
        private bool isVisible;

        [ObservableProperty]
        private bool isMapEditorAvailable;

        public ExtrasWindowViewModel(StatisticsWindowViewModel statisticsWindowViewModel)
        {
            this.statisticsWindowViewModel = statisticsWindowViewModel;
            IsMapEditorAvailable = !string.IsNullOrEmpty(ClientConfiguration.Instance.MapEditorExePath);
        }

        [RelayCommand]
        private void OpenStatistics()
        {
            IsVisible = false;
            statisticsWindowViewModel.IsVisible = true;
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

            IsVisible = false;
        }

        [RelayCommand]
        private void OpenCredits()
        {
            ProcessLauncher.StartShellProcess(ClientConfiguration.Instance.CreditsURL);
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }
    }
}


