using DXMainClientMVVMContract.Generic;

namespace DXMainClientView.Generic;

public interface IUpdateQueryWindowView
{
    IUpdateQueryWindowViewModel? ViewModel { get; set; }
}
