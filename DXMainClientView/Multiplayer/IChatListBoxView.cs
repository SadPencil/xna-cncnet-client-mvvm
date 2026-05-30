using DXMainClientMvvmContract.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
