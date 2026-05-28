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
    // --- Observable state ---

    [ObservableProperty]
    private string _draftMessage = string.Empty;

    [ObservableProperty]
    private bool _isAutoScrollEnabled = true;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _messages = new();
    public IReadOnlyList<string> Messages => _messages;

    // --- Events ---

    public event EventHandler<string>? MessageSendRequested;
    public event EventHandler<string>? LinkOpenRequested;
    public event EventHandler? ScrollToBottomRequested;

    // --- Constructor ---

    public ChatListBoxViewModel()
    {
    }

    // --- Commands ---

    [RelayCommand]
    private void SendMessage()
    {
        if (string.IsNullOrEmpty(DraftMessage))
            return;

        MessageSendRequested?.Invoke(this, DraftMessage);
        DraftMessage = string.Empty;
    }

    [RelayCommand]
    private void ClearMessages()
    {
        _messages.Clear();
    }

    [RelayCommand]
    private void ScrollToLatest()
    {
        ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    // --- Message management ---

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

        if (IsAutoScrollEnabled)
            ScrollToBottomRequested?.Invoke(this, EventArgs.Empty);
    }

    public void HandleLinkDoubleClick(string link)
    {
        if (!string.IsNullOrEmpty(link))
            LinkOpenRequested?.Invoke(this, link);
    }
}
