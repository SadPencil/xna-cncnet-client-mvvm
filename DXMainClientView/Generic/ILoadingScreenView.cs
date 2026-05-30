using DXMainClientMvvmContract.Generic;

namespace DXMainClientView.Generic;

public interface ILoadingScreenView
{
    ILoadingScreenViewModel? ViewModel { get; set; }
}
