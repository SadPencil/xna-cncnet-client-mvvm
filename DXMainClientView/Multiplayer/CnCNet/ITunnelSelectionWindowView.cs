using DXMainClientMvvmContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ITunnelSelectionWindowView
{
    ITunnelSelectionWindowViewModel? ViewModel { get; set; }
}
