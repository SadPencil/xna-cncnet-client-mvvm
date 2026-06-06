using System;
using System.Timers;

using AvClientMvvmContract.Multiplayer.CnCNet;
using AvClientMvvmContract.ViewServices;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer.CnCNet;


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
    public partial string SenderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MessagePreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    private readonly Action<string>? onOpenConversationRequested;

    // --- Constructor ---

    public PrivateMessageNotificationBoxViewModel(IUIThreadMarshaller uiThreadMarshaller, Action<string>? onOpenConversationRequested = null)
    {
        this.uiThreadMarshaller = uiThreadMarshaller;
        this.onOpenConversationRequested = onOpenConversationRequested;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenConversation()
    {
        Hide();
        onOpenConversationRequested?.Invoke(SenderName);
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
            uiThreadMarshaller.AddCallback(new Action(UI_Hide));
        };
        autoDismissTimer.Start();
    }

    /// <summary>
    /// Hides the notification box.
    /// </summary>
    public void Hide()
    {
        UI_Hide();
    }

    private void UI_Hide()
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

