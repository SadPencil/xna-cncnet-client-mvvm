using System;
using System.ComponentModel;

namespace AvClientMvvmContract.Generic;

public interface ILoadingScreenViewModel : INotifyPropertyChanged
{
    string StatusText { get; }
    string CurrentTaskText { get; }
    int ProgressPercentage { get; }
    bool IsIndeterminate { get; }
    bool IsLoading { get; }

    /// <summary>
    /// Whether borderless (fullscreen) client mode is enabled in INI settings.
    /// The MainWindow reads this before DI is ready to go fullscreen immediately.
    /// </summary>
    bool IsBorderlessClient { get; }
}
