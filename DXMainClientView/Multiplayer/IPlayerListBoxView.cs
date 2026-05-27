#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer;

namespace DXMainClientView.Multiplayer;

public interface IPlayerListBoxView
{
    IPlayerListBoxViewModel? ViewModel { get; set; }
}
