using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IGameCreationWindowView
{
    IGameCreationWindowViewModel? ViewModel { get; set; }
}
