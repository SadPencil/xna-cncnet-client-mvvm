using DXMainClientViewModel.Generic;

namespace DXMainClientView.Generic;

public interface ILoadingScreenView
{
    ILoadingScreenViewModel? ViewModel { get; set; }
}
