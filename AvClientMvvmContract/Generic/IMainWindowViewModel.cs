using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic;

public interface IMainWindowViewModel : INotifyPropertyChanged
{
    string WindowTitle { get; }
    WindowState WindowState { get; set; }

    /// <summary>
    /// Toggles between FullScreen and Normal window state (Alt+Enter).
    /// </summary>
    IRelayCommand ToggleFullScreenCommand { get; }

    /// <summary>
    /// Full path to the window icon file (clienticon.ico).
    /// </summary>
    string WindowIconPath { get; }
}
