using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

public interface IGameCreationWindowView
{
    IGameCreationWindowViewModel? ViewModel { get; set; }
}
