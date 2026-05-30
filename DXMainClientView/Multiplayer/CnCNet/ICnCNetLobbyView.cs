using DXMainClientMvvmContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ICnCNetLobbyView : ISwitchableView
{
    ICnCNetLobbyViewModel? ViewModel { get; set; }
}
