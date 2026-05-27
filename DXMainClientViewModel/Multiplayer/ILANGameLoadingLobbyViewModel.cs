#nullable enable

namespace DXMainClientViewModel;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
}
