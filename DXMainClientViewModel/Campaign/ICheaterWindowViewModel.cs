#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICheaterWindowViewModel
{
    string TitleText { get; }
    string MessageText { get; }

    IRelayCommand ConfirmCommand { get; }
    IRelayCommand CancelCommand { get; }
}
