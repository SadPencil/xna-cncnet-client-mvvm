using DXMainClientMVVMContract.Generic;

namespace DXMainClientView.Generic;

public interface IExtrasWindowView
{
    IExtrasWindowViewModel? ViewModel { get; set; }
}
