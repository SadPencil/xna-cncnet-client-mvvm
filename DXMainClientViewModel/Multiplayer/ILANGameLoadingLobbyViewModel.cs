#nullable enable

using DXMainClientViewModel;

namespace DXMainClientViewModel.Multiplayer;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
}
