using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
}
