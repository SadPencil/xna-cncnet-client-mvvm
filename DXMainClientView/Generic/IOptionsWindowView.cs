using DXMainClientMVVMContract.Generic;

namespace DXMainClientView.Generic;

public interface IOptionsWindowView
{
    IOptionsWindowViewModel? ViewModel { get; set; }
}
