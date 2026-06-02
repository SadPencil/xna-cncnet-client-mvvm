using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
}
