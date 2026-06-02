using AvMainClientMvvmContract.Generic;

namespace AvMainClientView.Generic;

public interface ITopBarView
{
    ITopBarViewModel? ViewModel { get; set; }
}
