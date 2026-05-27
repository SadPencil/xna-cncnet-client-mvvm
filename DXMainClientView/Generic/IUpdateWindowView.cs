#nullable enable

using System;

namespace DXMainClientView;

public interface IUpdateWindowView
{
    event Action? CancelRequested;

    void Show();
    void Hide();
    void SetDescription(string description);
    void SetCurrentFileName(string fileName);
    void SetCurrentFileProgress(int percentage);
    void SetTotalProgress(int percentage);
    void SetStatusText(string statusText);
    void SetCancelEnabled(bool enabled);
}
