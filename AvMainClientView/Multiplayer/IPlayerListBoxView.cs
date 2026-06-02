using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IPlayerListBoxView
{
    IPlayerListBoxViewModel? ViewModel { get; set; }
}
