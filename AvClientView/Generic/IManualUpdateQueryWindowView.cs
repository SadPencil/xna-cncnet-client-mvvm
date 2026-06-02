using AvClientMvvmContract.Generic;

namespace AvClientView.Generic;

public interface IManualUpdateQueryWindowView
{
    IManualUpdateQueryWindowViewModel? ViewModel { get; set; }
}
