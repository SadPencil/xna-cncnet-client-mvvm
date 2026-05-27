#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ITopBarView
{
    ITopBarViewModel? ViewModel { get; set; }
}
