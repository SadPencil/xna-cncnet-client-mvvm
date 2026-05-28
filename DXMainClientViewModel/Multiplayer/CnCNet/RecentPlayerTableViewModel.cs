using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Online;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

/// <summary>
/// ViewModel for the recent player table.
/// Contains all business logic from RecentPlayerTable.cs except XNA UI rendering.
/// </summary>
public partial class RecentPlayerTableViewModel : ObservableObject, IRecentPlayerTableViewModel
{
    private readonly CnCNetManager connectionManager;

    private readonly List<RecentPlayerEntry> recentPlayerEntries = new();

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedPlayerIndex = -1;

    [ObservableProperty]
    private string? _selectedPlayerName;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _recentPlayerNames = new();
    public IReadOnlyList<string> RecentPlayerNames => _recentPlayerNames;

    // --- Events ---

    /// <summary>
    /// Raised when a player is right-clicked (open action). The IRCUser is the target.
    /// </summary>
    public event EventHandler<RecentPlayerTableRightClickEventArgs>? PlayerRightClick;

    // --- Constructor ---

    public RecentPlayerTableViewModel(CnCNetManager connectionManager)
    {
        this.connectionManager = connectionManager;
    }

    // --- Commands ---

    [RelayCommand]
    private void OpenSelectedPlayer()
    {
        if (SelectedPlayerIndex < 0 || SelectedPlayerIndex >= recentPlayerEntries.Count)
            return;

        var entry = recentPlayerEntries[SelectedPlayerIndex];
        IRCUser ircUser = connectionManager.UserList.Find(u => u.Name == entry.PlayerName)
            ?? new IRCUser(entry.PlayerName);

        PlayerRightClick?.Invoke(this, new RecentPlayerTableRightClickEventArgs(ircUser));
    }

    [RelayCommand]
    private void RefreshRecentPlayers()
    {
        // Recent players are added externally via AddRecentPlayer.
        // This command can be used by the View to trigger a refresh if needed.
    }

    [RelayCommand]
    private void ClearRecentPlayers()
    {
        recentPlayerEntries.Clear();
        _recentPlayerNames.Clear();
        SelectedPlayerIndex = -1;
        SelectedPlayerName = null;
    }

    // --- Public methods ---

    /// <summary>
    /// Adds a recent player to the table.
    /// </summary>
    public void AddRecentPlayer(RecentPlayer recentPlayer)
    {
        bool isOnline = connectionManager.UserList.Any(u => u.Name == recentPlayer.PlayerName);

        var entry = new RecentPlayerEntry
        {
            PlayerName = recentPlayer.PlayerName,
            GameName = recentPlayer.GameName,
            GameTime = recentPlayer.GameTime.ToLocalTime().ToString("ddd, MMM d, yyyy @ h:mm tt"),
            IsOnline = isOnline
        };

        recentPlayerEntries.Add(entry);
        string onlineMarker = isOnline ? " [Online]" : string.Empty;
        _recentPlayerNames.Add($"{entry.PlayerName}{onlineMarker} - {entry.GameName} ({entry.GameTime})");
    }

    // --- Property change handlers ---

    partial void OnSelectedPlayerIndexChanged(int value)
    {
        if (value >= 0 && value < recentPlayerEntries.Count)
            SelectedPlayerName = recentPlayerEntries[value].PlayerName;
        else
            SelectedPlayerName = null;
    }

    // --- Inner types ---

    private class RecentPlayerEntry
    {
        public string PlayerName { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string GameTime { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
    }
}
