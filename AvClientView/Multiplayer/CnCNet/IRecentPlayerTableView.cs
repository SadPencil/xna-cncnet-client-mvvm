using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IRecentPlayerTableView
{
    IRecentPlayerTableViewModel? ViewModel { get; set; }
}
