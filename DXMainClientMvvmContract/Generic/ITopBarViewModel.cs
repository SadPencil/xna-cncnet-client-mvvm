using System.ComponentModel;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Generic;

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
    bool IsExpanded { get; }
    int UnreadMessageCount { get; }

    IRelayCommand SwitchToPrimaryCommand { get; }
    IRelayCommand SwitchToSecondaryCommand { get; }
    IRelayCommand SwitchToTertiaryCommand { get; }
    IRelayCommand OpenOptionsCommand { get; }
    IRelayCommand LogoutCommand { get; }
}

/// <summary>
/// Represents the type of view switch.
/// </summary>
public enum SwitchType
{
    PRIMARY,
    SECONDARY,
    PRIVATE_MESSAGES
}
