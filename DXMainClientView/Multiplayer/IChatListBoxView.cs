#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IChatListBoxView
{
    IChatListBoxViewModel? ViewModel { get; set; }
}
