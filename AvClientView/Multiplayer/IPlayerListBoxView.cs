using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IPlayerListBoxView
{
    IPlayerListBoxViewModel? ViewModel { get; set; }
}
