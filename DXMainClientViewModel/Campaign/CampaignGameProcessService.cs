using DXMainClientMVVMContract.Campaign;

using System;

namespace DXMainClientViewModel.Campaign;

/// <summary>
/// Implementation of ICampaignGameProcessService.
/// Delegates to IGameProcessService for the actual process lifecycle.
/// </summary>
public class CampaignGameProcessService : ICampaignGameProcessService
{
    private readonly IGameProcessService gameProcessService;

    public CampaignGameProcessService(IGameProcessService gameProcessService)
    {
        this.gameProcessService = gameProcessService;
        gameProcessService.GameProcessExited += OnGameProcessExited;
    }

    public event Action? GameProcessExited;

    public void StartGameProcess()
    {
        gameProcessService.StartGameProcess();
    }

    private void OnGameProcessExited()
    {
        GameProcessExited?.Invoke();
    }
}
