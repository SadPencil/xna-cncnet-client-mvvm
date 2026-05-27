using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IRecentPlayerTableView
{
    IRecentPlayerTableViewModel? ViewModel { get; set; }
}
