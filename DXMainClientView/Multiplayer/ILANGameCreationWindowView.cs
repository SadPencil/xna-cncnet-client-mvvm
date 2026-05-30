using DXMainClientMvvmContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
}
