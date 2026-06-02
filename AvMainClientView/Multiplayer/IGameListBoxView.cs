using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IGameListBoxView
{
    IGameListBoxViewModel? ViewModel { get; set; }
}
