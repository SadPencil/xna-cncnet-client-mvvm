#nullable enable

namespace DXMainClientView;

public interface ILANGameLobbyView : IMultiplayerGameLobbyView
{
    void SetLocalAddressText(string localAddressText);
    void SetDiscoveryStatusText(string statusText);
    void SetHostName(string hostName);
    void SetReadyButtonText(string text);
    void SetSharedStateText(string stateText);
}
