using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface IOptionsWindowView
{
    IOptionsWindowViewModel? ViewModel { get; set; }
}
