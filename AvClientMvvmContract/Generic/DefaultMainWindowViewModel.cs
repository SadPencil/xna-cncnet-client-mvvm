using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Generic;

/// <summary>
/// Minimal IMainWindowViewModel used before DI is ready.
/// The View creates this with the initial WindowState based on the
/// BorderlessWindowedClient INI setting so the window appears
/// fullscreen from the first frame. Replaced by the DI-created
/// MainWindowViewModel once ConnectAfterInit runs.
/// </summary>
public sealed class DefaultMainWindowViewModel : IMainWindowViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowState WindowState { get; set; }

    public IRelayCommand ToggleFullScreenCommand { get; } =
        new RelayCommand(() => { });

    public DefaultMainWindowViewModel(WindowState initialState)
    {
        WindowState = initialState;
    }
}
