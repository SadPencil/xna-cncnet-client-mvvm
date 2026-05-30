using DXMainClientMVVMContract.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface IPasswordRequestWindowView
{
    IPasswordRequestWindowViewModel? ViewModel { get; set; }
}
