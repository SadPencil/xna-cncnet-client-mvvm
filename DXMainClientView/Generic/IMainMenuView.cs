#nullable enable

using System;

namespace DXMainClientView;

public interface IMainMenuView : ISwitchableView
{
    event Action? CampaignRequested;
    event Action? LoadGameRequested;
    event Action? SkirmishRequested;
    event Action? CnCNetRequested;
    event Action? LanRequested;
    event Action? OptionsRequested;
    event Action? ExtrasRequested;
    event Action? ExitRequested;

    void SetOnlinePlayerCountText(string playerCountText);
    void SetVersionText(string versionText);
    void SetUpdateStatusText(string statusText);
    void SetUpdateStatusInteractive(bool interactive);
    void SetButtonEnabled(string buttonName, bool enabled);
    void ShowExtrasWindow();
}
