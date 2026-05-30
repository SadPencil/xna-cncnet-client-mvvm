using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IPlayerListBoxView
{
    IPlayerListBoxViewModel? ViewModel { get; set; }
}
