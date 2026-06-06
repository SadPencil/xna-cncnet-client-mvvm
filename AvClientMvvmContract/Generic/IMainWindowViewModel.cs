using System.ComponentModel;
using System.Windows.Input;

namespace AvClientMvvmContract.Generic;

public interface IMainWindowViewModel : INotifyPropertyChanged
{
    WindowState WindowState { get; set; }

    /// <summary>
    /// Toggles between FullScreen and Normal window state (Alt+Enter).
    /// </summary>
    ICommand ToggleFullScreenCommand { get; }
}
