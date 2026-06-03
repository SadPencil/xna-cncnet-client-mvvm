namespace AvClientMvvmContract.Generic;

/// <summary>
/// Message sent via WeakReferenceMessenger by any ViewModel to request an OK dialog.
/// The View receives this message and renders the dialog UI.
/// </summary>
public interface IOKDialogMessage
{
    string Title { get; }
    string Message { get; }
}
