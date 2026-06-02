using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IRecentPlayerTableView
{
    IRecentPlayerTableViewModel? ViewModel { get; set; }
}
