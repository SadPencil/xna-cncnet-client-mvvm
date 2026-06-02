using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface ILANLobbyView : ISwitchableView
{
    ILANLobbyViewModel? ViewModel { get; set; }
}
