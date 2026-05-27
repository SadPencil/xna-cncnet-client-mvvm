#nullable enable
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IManualUpdateQueryWindowViewModel : INotifyPropertyChanged
{
    string VersionText { get; }
    string DownloadUrl { get; }

    IRelayCommand OpenDownloadPageCommand { get; }
    IRelayCommand CloseCommand { get; }
}
