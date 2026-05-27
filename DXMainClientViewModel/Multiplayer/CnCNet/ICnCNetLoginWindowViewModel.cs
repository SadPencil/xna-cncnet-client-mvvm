#nullable enable
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel;

public interface ICnCNetLoginWindowViewModel
{
    string UserName { get; set; }
    string Password { get; set; }
    bool RememberPassword { get; set; }
    bool PersistentMode { get; set; }
    bool AutoConnect { get; set; }

    IAsyncRelayCommand ConnectCommand { get; }
    IRelayCommand CancelCommand { get; }
}
