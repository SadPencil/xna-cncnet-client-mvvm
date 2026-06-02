using System;
using System.ComponentModel;

namespace AvMainClientMvvmContract.Generic;

public interface ILoadingScreenViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }
    bool IsLoading { get; }
}
