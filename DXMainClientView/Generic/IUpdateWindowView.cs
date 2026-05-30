using DXMainClientMVVMContract.Generic;

namespace DXMainClientView.Generic;

public interface IUpdateWindowView
{
    IUpdateWindowViewModel? ViewModel { get; set; }
}
