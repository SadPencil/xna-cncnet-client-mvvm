using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
