using System;

namespace AvClientMvvmContract.Multiplayer.CnCNet;

/// <summary>
/// Read-only view of data for a pending yes/no dialog.
/// </summary>
public interface IPendingYesNoDialogData
{
    string Title { get; }
    string Text { get; }
}
