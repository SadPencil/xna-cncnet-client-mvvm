using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
}
