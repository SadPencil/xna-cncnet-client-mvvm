using System.ComponentModel;

namespace DXMainClientViewModel.Generic;

public interface ILoadingScreenViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }
    bool IsLoading { get; }
}
