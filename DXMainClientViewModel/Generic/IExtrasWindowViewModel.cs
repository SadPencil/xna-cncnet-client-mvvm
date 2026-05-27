#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IExtrasWindowViewModel
{
    bool IsStatisticsAvailable { get; }
    bool IsMapEditorAvailable { get; }

    IRelayCommand OpenStatisticsCommand { get; }
    IAsyncRelayCommand OpenMapEditorCommand { get; }
    IRelayCommand OpenCreditsCommand { get; }
    IRelayCommand CloseCommand { get; }
}
