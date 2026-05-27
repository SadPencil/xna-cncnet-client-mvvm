#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IManualUpdateQueryWindowViewModel
{
    string VersionText { get; }
    string DownloadUrl { get; }

    IRelayCommand OpenDownloadPageCommand { get; }
    IRelayCommand CloseCommand { get; }
}
