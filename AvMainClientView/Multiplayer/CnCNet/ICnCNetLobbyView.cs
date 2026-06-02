using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLobbyView : ISwitchableView
{
    ICnCNetLobbyViewModel? ViewModel { get; set; }
}
