#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IMultiplayerGameLobbyViewModel : IGameLobbyViewModel
{
    IReadOnlyList<string> ChatMessages { get; }
    string DraftMessage { get; set; }
    bool IsReady { get; set; }
    bool IsGameLocked { get; }

    IRelayCommand SendChatMessageCommand { get; }
    IRelayCommand ToggleReadyCommand { get; }
    IRelayCommand LockGameCommand { get; }
}
