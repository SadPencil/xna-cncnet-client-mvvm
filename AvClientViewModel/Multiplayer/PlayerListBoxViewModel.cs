using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Multiplayer;

using AvClientViewModel.Domain.Multiplayer.CnCNet;
using AvClientViewModel.Online;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer;

/// <summary>
/// ViewModel for the player list box.
/// Contains all business logic from PlayerListBox.cs except XNA UI rendering.
/// </summary>
public partial class PlayerListBoxViewModel : ObservableObject, IPlayerListBoxViewModel
{
    private readonly GameCollection gameCollection;
    private readonly Action<ChannelUser>? onPlayerOpened;

    // --- State ---

    private readonly List<ChannelUser> users = new();

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedPlayerIndex = -1;

    [ObservableProperty]
    private string? _selectedPlayerName;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _playerNames = new();
    public IReadOnlyList<string> PlayerNames => _playerNames;

    // --- Constructor ---

    public PlayerListBoxViewModel(GameCollection gameCollection, Action<ChannelUser>? onPlayerOpened = null)
    {
        this.gameCollection = gameCollection;
        this.onPlayerOpened = onPlayerOpened;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenSelectedPlayer()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= users.Count)
            return;

        onPlayerOpened?.Invoke(users[SelectedPlayerIndex]);
    }

    [RelayCommand]
    private void RefreshPlayers()
    {
        RefreshPlayerNames();
    }

    // --- Property change handlers ---

    partial void OnSelectedPlayerIndexChanged(int value)
    {
        if (value >= 0 && value < users.Count)
            SelectedPlayerName = users[value].IRCUser.Name;
        else
            SelectedPlayerName = null;
    }

    // --- User management (called by parent ViewModel) ---

    public void AddUser(ChannelUser user)
    {
        users.Add(user);
        _playerNames.Add(GetDisplayName(user));
    }

    public void RemoveUser(ChannelUser user)
    {
        int index = users.IndexOf(user);
        if (index >= 0)
        {
            users.RemoveAt(index);
            _playerNames.RemoveAt(index);

            if (SelectedPlayerIndex >= users.Count)
                SelectedPlayerIndex = users.Count > 0 ? users.Count - 1 : -1;
        }
    }

    public void UpdateUserInfo(ChannelUser user)
    {
        int index = users.IndexOf(user);
        if (index >= 0)
        {
            _playerNames[index] = GetDisplayName(user);
        }
    }

    public void ClearUsers()
    {
        users.Clear();
        _playerNames.Clear();
        SelectedPlayerIndex = -1;
    }

    public ChannelUser? GetUserAtIndex(int index)
    {
        return index >= 0 && index < users.Count ? users[index] : null;
    }

    public int GetUserCount() => users.Count;

    // --- Helpers ---

    private string GetDisplayName(ChannelUser user)
    {
        if (user.IsAdmin)
            return user.IRCUser.Name + " " + "(Admin)".L10N("Client:Main:AdminSuffix");

        return user.IRCUser.Name;
    }

    private void RefreshPlayerNames()
    {
        _playerNames.Clear();
        foreach (var user in users)
            _playerNames.Add(GetDisplayName(user));
    }
}

