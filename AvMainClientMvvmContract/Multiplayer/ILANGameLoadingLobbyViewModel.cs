namespace AvMainClientMvvmContract.Multiplayer;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
    bool IsEnabled { get; }
}
