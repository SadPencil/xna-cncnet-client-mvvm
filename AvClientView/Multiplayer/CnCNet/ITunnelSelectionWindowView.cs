using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface ITunnelSelectionWindowView
{
    ITunnelSelectionWindowViewModel? ViewModel { get; set; }
}
