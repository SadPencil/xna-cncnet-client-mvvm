namespace AvClientMvvmContract.Multiplayer;

public interface ILANGameLoadingLobbyViewModel : IGameLoadingLobbyViewModel
{
    bool IsVisible { get; set; }
    string LocalAddressText { get; }
    bool AreAllPlayersReady { get; }
    bool IsEnabled { get; }
}
