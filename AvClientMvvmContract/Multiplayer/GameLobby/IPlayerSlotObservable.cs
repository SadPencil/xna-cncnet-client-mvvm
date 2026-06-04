using System.ComponentModel;

namespace AvClientMvvmContract.Multiplayer.GameLobby;

/// <summary>
/// Observable state for one player slot row in the game lobby.
/// Each dropdown (Name/Side/Color/Start/Team) is an IPlayerSlotDropdown
/// with its own Options, SelectedOption, and IsEnabled — no indices needed.
/// </summary>
public interface IPlayerSlotObservable : INotifyPropertyChanged
{
    string PlayerName { get; set; }

    IPlayerSlotDropdown Name { get; set; }
    IPlayerSlotDropdown Side { get; set; }
    IPlayerSlotDropdown Color { get; set; }
    IPlayerSlotDropdown Start { get; set; }
    IPlayerSlotDropdown Team { get; set; }
}
