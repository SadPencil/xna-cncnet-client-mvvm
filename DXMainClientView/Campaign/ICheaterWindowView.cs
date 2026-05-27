#nullable enable

using System;

namespace DXMainClientView;

public interface ICheaterWindowView
{
    event Action? Confirmed;
    event Action? Cancelled;

    void Show();
    void Hide();
    void SetTitle(string title);
    void SetWarningText(string warningText);
    void SetConfirmationEnabled(bool enabled);
}
