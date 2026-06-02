using AvMainClientMvvmContract.Multiplayer.CnCNet;

namespace AvMainClientView.Multiplayer.CnCNet;

public interface IGameCreationWindowView
{
    IGameCreationWindowViewModel? ViewModel { get; set; }
}
