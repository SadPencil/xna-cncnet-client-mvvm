#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPlayerListBoxView
{
    IPlayerListBoxViewModel? ViewModel { get; set; }
}
