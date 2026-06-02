using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface ICnCNetLobbyView : ISwitchableView
{
    ICnCNetLobbyViewModel? ViewModel { get; set; }
}
