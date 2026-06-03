namespace AvClientMvvmContract.Generic;

/// <summary>
/// Message sent via WeakReferenceMessenger by any ViewModel to request a Yes/No dialog.
/// The View receives this message, renders the dialog, and calls Reply() with the user's choice.
/// The sender can await the response.
/// </summary>
public interface IYesNoDialogMessage
{
    string Title { get; }
    string Message { get; }
    void Reply(bool response);
    bool HasReceivedResponse { get; }
}
