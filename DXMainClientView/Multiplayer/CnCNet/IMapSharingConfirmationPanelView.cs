#nullable enable

using System;

namespace DXMainClientView;

public interface IMapSharingConfirmationPanelView
{
    event Action? AcceptRequested;
    event Action? RejectRequested;

    void Show();
    void Hide();
    void SetMapName(string mapName);
    void SetStatusText(string statusText);
    void SetDownloadProgress(int percentage);
    void SetAcceptEnabled(bool enabled);
    void SetRejectVisible(bool visible);
}
