using DXMainClientMVVMContract.Multiplayer.CnCNet;
using System;
using System.Timers;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DXMainClientMVVMContract.ViewServices;

namespace DXMainClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the choice notification box.
/// Contains all business logic from ChoiceNotificationBox.cs except XNA UI rendering.
/// </summary>
public partial class ChoiceNotificationBoxViewModel : ObservableObject, IChoiceNotificationBoxViewModel
{
    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private Timer? autoDismissTimer;

    // --- Observable state ---

    [ObservableProperty]
    private string _titleText = string.Empty;

    [ObservableProperty]
    private string _senderName = string.Empty;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private string _acceptButtonText = string.Empty;

    [ObservableProperty]
    private string _declineButtonText = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    private readonly Action<string>? onAffirmativeClicked;
    private readonly Action<string>? onNegativeClicked;

    // --- Constructor ---

    public ChoiceNotificationBoxViewModel(IUIThreadMarshaller uiThreadMarshaller, Action<string>? onAffirmativeClicked = null, Action<string>? onNegativeClicked = null)
    {
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.onAffirmativeClicked = onAffirmativeClicked;
        this.onNegativeClicked = onNegativeClicked;
    }

    // --- Commands ---

    [RelayCommand]
    private void Accept()
    {
        Hide();
        onAffirmativeClicked?.Invoke(SenderName);
    }

    [RelayCommand]
    private void Decline()
    {
        Hide();
        onNegativeClicked?.Invoke(SenderName);
    }

    // --- Public methods ---

    /// <summary>
    /// Shows the notification box with the specified content.
    /// A timeout of zero means the notification will never be automatically dismissed.
    /// </summary>
    public void Show(
        string headerText,
        string sender,
        string choiceText,
        string affirmativeText,
        string negativeText,
        int timeout = 0)
    {
        TitleText = headerText;
        SenderName = sender;
        MessageText = choiceText;
        AcceptButtonText = affirmativeText;
        DeclineButtonText = negativeText;
        IsVisible = true;

        StopAutoDismissTimer();

        if (timeout > 0)
        {
            autoDismissTimer = new Timer(timeout * 1000.0);
            autoDismissTimer.AutoReset = false;
            autoDismissTimer.Elapsed += (s, e) =>
            {
                uiThreadMarshaller.AddCallback(new Action(Hide));
            };
            autoDismissTimer.Start();
        }
    }

    /// <summary>
    /// Hides the notification box.
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
        StopAutoDismissTimer();
    }

    // --- Helpers ---

    private void StopAutoDismissTimer()
    {
        if (autoDismissTimer != null)
        {
            autoDismissTimer.Stop();
            autoDismissTimer.Dispose();
            autoDismissTimer = null;
        }
    }
}

