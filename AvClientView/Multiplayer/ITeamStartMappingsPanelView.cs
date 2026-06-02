using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface ITeamStartMappingsPanelView
{
    ITeamStartMappingsPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
