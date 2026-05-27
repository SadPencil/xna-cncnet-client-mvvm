#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface IPasswordRequestWindowViewModel
{
    string GameName { get; }
    string HostName { get; }
    string Password { get; set; }

    IRelayCommand SubmitPasswordCommand { get; }
    IRelayCommand CancelCommand { get; }
}
