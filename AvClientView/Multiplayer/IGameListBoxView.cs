using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IGameListBoxView
{
    IGameListBoxViewModel? ViewModel { get; set; }
}
