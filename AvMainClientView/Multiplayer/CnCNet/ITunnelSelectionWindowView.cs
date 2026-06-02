using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface ITunnelSelectionWindowView
{
    ITunnelSelectionWindowViewModel? ViewModel { get; set; }
}
