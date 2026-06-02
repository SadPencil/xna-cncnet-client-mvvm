using AvMainClientMvvmContract.Multiplayer;

namespace AvMainClientView.Multiplayer;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
