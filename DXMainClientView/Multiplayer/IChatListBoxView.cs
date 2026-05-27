#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
