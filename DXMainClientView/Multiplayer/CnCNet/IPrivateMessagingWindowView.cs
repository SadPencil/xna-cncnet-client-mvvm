#nullable enable

using DXMainClientViewModel;

namespace DXMainClientView;

public interface IPrivateMessagingWindowView : ISwitchableView
{
    IPrivateMessagingWindowViewModel? ViewModel { get; set; }
}
