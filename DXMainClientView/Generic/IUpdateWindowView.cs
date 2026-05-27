#nullable enable

using DXMainClientView;

using DXMainClientViewModel;
using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface IUpdateWindowView
{
    IUpdateWindowViewModel? ViewModel { get; set; }
    void Show();
    void Hide();
}
