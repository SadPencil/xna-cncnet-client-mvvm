#nullable enable

using System;

namespace DXMainClientView;

public interface IManualUpdateQueryWindowView
{
    event Action? DownloadRequested;
    event Action? CloseRequested;

    void Show();
    void Hide();
    void SetVersion(string version);
    void SetDownloadUrl(string downloadUrl);
    void SetDownloadEnabled(bool enabled);
}
