using DXMainClientMvvmContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ITeamStartMappingsPanelView
{
    ITeamStartMappingsPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
