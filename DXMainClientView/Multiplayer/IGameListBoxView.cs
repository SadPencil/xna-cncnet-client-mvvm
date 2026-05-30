using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IGameListBoxView
{
    IGameListBoxViewModel? ViewModel { get; set; }
}
