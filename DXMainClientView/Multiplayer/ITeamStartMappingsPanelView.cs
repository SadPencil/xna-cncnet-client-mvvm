#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ITeamStartMappingsPanelView
{
    ITeamStartMappingsPanelViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
