using AvMainClientMvvmContract.Multiplayer;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using AvMainClientViewModel.Online;
using AvMainClientMvvmContract.ViewServices;
using AvMainClientMvvmContract;

namespace AvMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the chat list box.
/// Contains all business logic from ChatListBox.cs except XNA UI rendering.
/// </summary>
public partial class ChatListBoxViewModel : ObservableObject, IChatListBoxViewModel
{
    private readonly Action<string>? onMessageSend;
    private readonly IUrlService urlService;

    // --- Observable state ---

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    [ObservableProperty]
    private string? _pendingUntrustedUrl;

    [ObservableProperty]
    private bool _isUntrustedUrlDialogVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _messages = new();
    public IReadOnlyList<string> Messages => _messages;

    // --- Constructor ---

    public ChatListBoxViewModel(IUrlService urlService, Action<string>? onMessageSend = null)
    {
        this.urlService = urlService;
        this.onMessageSend = onMessageSend;
    }

    // --- Commands ---

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage))
            return;

        onMessageSend?.Invoke(DraftMessage);
        DraftMessage = string.Empty;
    }

    [RelayCommand]
    private void ClearMessages()
    {
        _messages.Clear();
    }

    [RelayCommand]
    private void OpenLink(string? link)
    {
        if (string.IsNullOrEmpty(link))
            return;

        if (urlService.IsTrustedUrl(link))
        {
            urlService.OpenUrl(link);
        }
        else
        {
            PendingUntrustedUrl = link;
            IsUntrustedUrlDialogVisible = true;
        }
    }

    [RelayCommand]
    private void ConfirmOpenUrl()
    {
        if (!string.IsNullOrEmpty(PendingUntrustedUrl))
            urlService.OpenUrl(PendingUntrustedUrl);

        PendingUntrustedUrl = null;
        IsUntrustedUrlDialogVisible = false;
    }

    [RelayCommand]
    private void CancelOpenUrl()
    {
        PendingUntrustedUrl = null;
        IsUntrustedUrlDialogVisible = false;
    }

    // --- Message management (called by parent ViewModel) ---

    public void AddMessage(string message)
    {
        AddMessage(new ChatMessage(message));
    }

    public void AddMessage(string sender, string message, IRgb24Color color)
    {
        AddMessage(new ChatMessage(sender, color, DateTime.Now, message));
    }

    public void AddMessage(ChatMessage message)
    {
        string formattedMessage;

        if (message.SenderName == null)
        {
            formattedMessage = string.Format("[{0}] {1}",
                message.DateTime.ToShortTimeString(),
                message.Message);
        }
        else
        {
            formattedMessage = string.Format("[{0}] {1}: {2}",
                message.DateTime.ToShortTimeString(),
                message.SenderName,
                message.Message);
        }

        _messages.Add(formattedMessage);
        // View observes Messages collection changes + IsAutoScrollEnabled to auto-scroll
    }
}

