using System;
using System.ComponentModel;

namespace DXMainClientMvvmContract.Generic;

public interface ILoadingScreenViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }
    bool IsLoading { get; }

    event EventHandler Completed;
}
