using DXMainClientMvvmContract.Generic;

namespace DXMainClientView.Generic;

public interface ITopBarView
{
    ITopBarViewModel? ViewModel { get; set; }
}
