#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IGameListBoxView
{
    IGameListBoxViewModel? ViewModel { get; set; }
}
