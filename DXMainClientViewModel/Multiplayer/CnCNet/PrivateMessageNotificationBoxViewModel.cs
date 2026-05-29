using System;
using System.Timers;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the private message notification box.
/// Contains all business logic from PrivateMessageNotificationBox.cs except XNA UI rendering.
/// </summary>
public partial class PrivateMessageNotificationBoxViewModel : ObservableObject, IPrivateMessageNotificationBoxViewModel
{
    private const double AUTO_DISMISS_SECONDS = 4.0;

    private readonly IUIThreadMarshaller uiThreadMarshaller;
    private Timer? autoDismissTimer;

    // --- Observable state ---

    [ObservableProperty]
    private string _senderName = string.Empty;

    [ObservableProperty]
    private string _messagePreview = string.Empty;

    [ObservableProperty]
    private bool _isVisible;

    // --- Events ---

    /// <summary>
    /// Raised when the user wants to open the conversation with the sender.
    /// </summary>
    public event EventHandler<string>? OpenConversationRequested;

    // --- Constructor ---

    public PrivateMessageNotificationBoxViewModel(IUIThreadMarshaller uiThreadMarshaller)
    {
        this.uiThreadMarshaller = uiThreadMarshaller;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenConversation()
    {
        Hide();
        OpenConversationRequested?.Invoke(this, SenderName);
    }

    [RelayCommand]
    private void Dismiss()
    {
        Hide();
    }

    // --- Public methods ---

    /// <summary>
    /// Shows the notification with the specified sender and message.
    /// Auto-dismisses after 4 seconds.
    /// </summary>
    public void Show(string sender, string message)
    {
        SenderName = sender;
        MessagePreview = message;
        IsVisible = true;

        StopAutoDismissTimer();

        autoDismissTimer = new Timer(AUTO_DISMISS_SECONDS * 1000.0);
        autoDismissTimer.AutoReset = false;
        autoDismissTimer.Elapsed += (s, e) =>
        {
            uiThreadMarshaller.AddCallback(new Action(Hide));
        };
        autoDismissTimer.Start();
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
// checked
