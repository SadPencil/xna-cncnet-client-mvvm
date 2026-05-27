#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface ILANGameCreationWindowView
{
    ILANGameCreationWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
