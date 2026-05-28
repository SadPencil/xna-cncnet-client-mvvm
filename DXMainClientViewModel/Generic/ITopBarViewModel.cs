using System;
using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Generic;

public interface ITopBarViewModel : INotifyPropertyChanged
{
    string ConnectionStatusText { get; }
    string PlayerCountText { get; }
    bool IsPlayerCountVisible { get; }
    string PlayerCountLabel { get; }
    bool AreSwitchButtonsClickable { get; }
    bool IsOptionsButtonClickable { get; }
    bool IsLogoutButtonClickable { get; }
    bool IsLanMode { get; set; }
    SwitchType LastSwitchType { get; }
    string MainButtonText { get; }

    IRelayCommand SwitchToPrimaryCommand { get; }
    IRelayCommand SwitchToSecondaryCommand { get; }
    IRelayCommand SwitchToTertiaryCommand { get; }
    IRelayCommand OpenOptionsCommand { get; }
    IRelayCommand LogoutCommand { get; }

    /// <summary>
    /// Raised when the top bar should animate down.
    /// </summary>
    event Action? BringDownRequested;

    /// <summary>
    /// Raised when logout is performed. View should handle navigation.
    /// </summary>
    event Action? LogoutPerformed;

    void Initialize();

    /// <summary>
    /// Cleans up resources.
    /// </summary>
    void Clean();
}

/// <summary>
/// Represents the type of view switch.
/// </summary>
public enum SwitchType
{
    PRIMARY,
    SECONDARY
}
