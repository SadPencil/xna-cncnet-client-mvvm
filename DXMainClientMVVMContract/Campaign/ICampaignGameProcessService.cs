using System;

namespace DXMainClientMVVMContract.Campaign;

/// <summary>
/// Service for launching the campaign game process and tracking its lifecycle.
/// Abstracts GameProcessLogic (which lives in ClientGUI) for ViewModel use.
/// </summary>
public interface ICampaignGameProcessService
{
    /// <summary>
    /// Fired when the game process exits.
    /// </summary>
    event Action? GameProcessExited;

    /// <summary>
    /// Starts the game process. The spawn.ini must already be prepared.
    /// </summary>
    void StartGameProcess();
}
