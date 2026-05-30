using DXMainClientMVVMContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
