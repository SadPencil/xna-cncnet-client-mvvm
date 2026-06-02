using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface ITeamStartMappingsPanelView
{
    ITeamStartMappingsPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
