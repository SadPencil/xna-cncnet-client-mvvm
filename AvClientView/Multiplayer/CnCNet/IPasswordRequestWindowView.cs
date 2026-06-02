using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IPasswordRequestWindowView
{
    IPasswordRequestWindowViewModel? ViewModel { get; set; }
}
