using System;
using System.Collections.Generic;

using CommunityToolkit.Mvvm.Input;

namespace DXMainClientMvvmContract.Multiplayer.GameLobby;

public interface IMultiplayerGameLobbyViewModel : IGameLobbyViewModel
{
    // --- Chat ---
    IReadOnlyList<string> ChatMessages { get; }
    string DraftMessage { get; set; }

    // --- Ready / Lock ---
    bool IsReady { get; set; }
    bool IsGameLocked { get; }
    bool IsAutoReadyChecked { get; set; }
    bool IsAutoReadyEnabled { get; }

    // --- Network settings ---
    int FrameSendRate { get; }
    int MaxAhead { get; }
    int ProtocolVersion { get; }

    // --- Player status (for status indicators, ping display) ---
    IReadOnlyList<PlayerSlotState> PlayerStatuses { get; }
    IReadOnlyList<string> PlayerStatusTooltips { get; }
    IReadOnlyList<int> PlayerPings { get; }

    // --- Map list visibility (host has map list, player does not) ---
    bool IsMapListVisible { get; }

    // --- Map preview start location selection ---
    bool IsStartLocationSelectionEnabled { get; }

    // --- Launch button ---
    string LockGameButtonText { get; }
    bool IsLockGameButtonVisible { get; }

    // --- Save game notification ---
    bool HasSavedGameWarning { get; }

    // --- Commands ---
    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand ToggleReadyCommand { get; }
    IRelayCommand LockGameCommand { get; }
    IRelayCommand StartingLocationAppliedCommand { get; }

    // --- Warning notices ---
    void AddWarning(string message);
}
