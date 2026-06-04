using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable state for one player slot row in the game lobby.
/// Each dropdown is typed so the ViewModel matches by Index, not
/// by translated display names.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    string PlayerName { get; set; }

    IPlayerSlotDropdown<IPlayerName> Name { get; set; }
    IPlayerSlotDropdown<IPlayerSide> Side { get; set; }
    IPlayerSlotDropdown<IPlayerColor> Color { get; set; }
    IPlayerSlotDropdown<IPlayerStart> Start { get; set; }
    IPlayerSlotDropdown<IPlayerTeam> Team { get; set; }
}
