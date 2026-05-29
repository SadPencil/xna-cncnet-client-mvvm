
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the chat list box.
/// Contains all business logic from ChatListBox.cs except XNA UI rendering.
/// </summary>
public partial class ChatListBoxViewModel : ObservableObject, IChatListBoxViewModel
{
    private readonly Action<string>? onMessageSend;
    private readonly Action<string>? onLinkOpen;

    // --- Observable state ---

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _messages = new();
    public IReadOnlyList<string> Messages => _messages;

    // --- Constructor ---

    public ChatListBoxViewModel(Action<string>? onMessageSend = null, Action<string>? onLinkOpen = null)
    {
        this.onMessageSend = onMessageSend;
        this.onLinkOpen = onLinkOpen;
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
        if (!string.IsNullOrEmpty(link))
            onLinkOpen?.Invoke(link);
    }

    // --- Message management (called by parent ViewModel) ---

    public void AddMessage(string message)
    {
        AddMessage(new ChatMessage(message));
    }

    public void AddMessage(string sender, string message, int r, int g, int b)
    {
        AddMessage(new ChatMessage(sender, r, g, b, DateTime.Now, message));
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

