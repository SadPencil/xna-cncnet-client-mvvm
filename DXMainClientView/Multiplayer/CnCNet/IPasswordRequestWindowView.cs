#nullable enable

using System;

namespace DXMainClientView;

public interface IPasswordRequestWindowView
{
    event Action? PasswordSubmitted;
    event Action? CancelRequested;

    void Show();
    void Hide();
    void SetRoomName(string roomName);
    void ClearPassword();
    void SetSubmitEnabled(bool enabled);
    void ShowError(string errorMessage);
}
