using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface ILANLobbyView : ISwitchableView
{
    ILANLobbyViewModel? ViewModel { get; set; }
}
