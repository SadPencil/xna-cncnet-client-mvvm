#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ITunnelListBoxView
{
    ITunnelListBoxViewModel? ViewModel { get; set; }
}
