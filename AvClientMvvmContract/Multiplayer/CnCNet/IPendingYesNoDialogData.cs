using CommunityToolkit.Mvvm.Input;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

/// <summary>
/// Read-only view of data for a pending yes/no dialog.
/// The View binds Yes/No buttons to the commands, which invoke the callback.
/// </summary>
public interface IPendingYesNoDialogData
{
    string Title { get; }
    string Text { get; }
    IRelayCommand YesCommand { get; }
    IRelayCommand NoCommand { get; }
}
