#nullable enable

namespace DXMainClientView;

public interface ILANGameLoadingLobbyView : IGameLoadingLobbyView
{
    void SetBroadcastStatusText(string statusText);
    void SetReadyButtonEnabled(bool enabled);
    void SetDiscoveryStatusText(string statusText);
    void AddSystemMessage(string message);
    void SetHostPlayerName(string hostPlayerName);
}
