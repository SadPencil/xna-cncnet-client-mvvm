#nullable enable

using System;

namespace DXMainClientView;

public interface ILANGameCreationWindowView
{
    event Action? CreateNewGameRequested;
    event Action? LoadSavedGameRequested;
    event Action? CancelRequested;

    void Show();
    void Hide();
    void SetTitle(string title);
    void SetCreateEnabled(bool enabled);
    void SetLoadEnabled(bool enabled);
}
