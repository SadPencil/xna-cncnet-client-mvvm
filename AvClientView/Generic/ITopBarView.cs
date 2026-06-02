using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface ITopBarView
{
    ITopBarViewModel? ViewModel { get; set; }
}
