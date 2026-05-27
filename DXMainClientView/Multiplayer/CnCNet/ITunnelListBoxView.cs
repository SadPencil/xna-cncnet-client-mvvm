#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public interface ITunnelListBoxView
{
    ITunnelListBoxViewModel? ViewModel { get; set; }
}
