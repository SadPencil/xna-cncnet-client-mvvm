#nullable enable

using System;

namespace DXMainClientView;

public interface IUpdateQueryWindowView
{
    event Action? UpdateAccepted;
    event Action? UpdateDeclined;

    void Show();
    void Hide();
    void SetVersion(string version);
    void SetUpdateSizeText(string updateSizeText);
    void SetAcceptEnabled(bool enabled);
}
