using System;

using AvClientMvvmContract.Generic;
using AvClientMvvmContract.Messages;

using ClientCore.Settings;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AvClientViewModel.Generic;

/// <summary>
/// ViewModel for the MainWindow. Determines effective WindowState:
///   1. Game running + MinimizeWindowsOnGameStart → Minimized
///   2. Alt+Enter user override (FullScreen toggle)
///   3. BorderlessWindowedClient → FullScreen
///   4. Otherwise → user preference (Normal or Maximized from TwoWay binding)
/// </summary>
public partial class MainWindowViewModel : ObservableObject, IMainWindowViewModel, IRecipient<BorderlessClientToggledMessage>
{
    private readonly IGameInProgressWindowViewModel? gameInProgressViewModel;

    private bool _isUpdatingState;
    private WindowState _lastUserState = WindowState.Normal;

    /// <summary>
    /// null = no override, use borderless setting.
    /// true = Alt+Enter forced fullscreen.
    /// false = Alt+Enter forced normal.
    /// </summary>
    private bool? _userFullScreenOverride;

    [ObservableProperty]
    public partial WindowState WindowState { get; set; }

    /// <summary>
    /// When <paramref name="gameInProgressViewModel"/> is null (before DI is ready),
    /// the view model only handles borderless fullscreen. The real
    /// view model with game-in-progress support replaces it during ConnectAfterInit.
    /// </summary>
    public MainWindowViewModel(IGameInProgressWindowViewModel? gameInProgressViewModel)
    {
        this.gameInProgressViewModel = gameInProgressViewModel;

        WeakReferenceMessenger.Default.Register(this);

        ApplyEffectiveState();

        if (gameInProgressViewModel != null)
        {
            gameInProgressViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IGameInProgressWindowViewModel.IsGameInProgress))
                    ApplyEffectiveState();
            };
        }
    }

    public void Receive(BorderlessClientToggledMessage message)
    {
        // Clear Alt+Enter override when user changes the INI setting
        _userFullScreenOverride = null;
        ApplyEffectiveState();
    }

    /// <summary>
    /// Alt+Enter toggle. Flips between FullScreen and Normal, recording
    /// the user's preference so it persists across minimize/restore cycles.
    /// </summary>
    [RelayCommand]
    private void ToggleFullScreen()
    {
        if (WindowState == WindowState.FullScreen)
        {
            _userFullScreenOverride = false;
            _lastUserState = WindowState.Normal;
        }
        else
        {
            _userFullScreenOverride = true;
        }

        ApplyEffectiveState();
    }

    partial void OnWindowStateChanged(WindowState value)
    {
        if (_isUpdatingState)
            return;

        if (value == WindowState.Normal || value == WindowState.Maximized)
        {
            _lastUserState = value;
            // User manually resized — clear fullscreen override
            _userFullScreenOverride = null;
        }
    }

    private void ApplyEffectiveState()
    {
        _isUpdatingState = true;
        try
        {
            if (gameInProgressViewModel != null
                && gameInProgressViewModel.IsGameInProgress
                && UserINISettings.Instance.MinimizeWindowsOnGameStart)
            {
                WindowState = WindowState.Minimized;
            }
            else if (_userFullScreenOverride == true)
            {
                WindowState = WindowState.FullScreen;
            }
            else if (_userFullScreenOverride == false)
            {
                WindowState = _lastUserState;
            }
            else if (UserINISettings.Instance.BorderlessWindowedClient)
            {
                WindowState = WindowState.FullScreen;
            }
            else
            {
                WindowState = _lastUserState;
            }
        }
        finally
        {
            _isUpdatingState = false;
        }
    }

}
