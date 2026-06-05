using System.ComponentModel;

namespace AvClientMvvmContract.Generic;

public interface IMainWindowViewModel : INotifyPropertyChanged
{
    WindowState WindowState { get; }
}
