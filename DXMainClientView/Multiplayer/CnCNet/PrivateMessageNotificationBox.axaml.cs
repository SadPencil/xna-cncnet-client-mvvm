using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class PrivateMessageNotificationBox : UserControl, IPrivateMessageNotificationBoxView
{
    public PrivateMessageNotificationBox()
    {
        InitializeComponent();
    }

    public IPrivateMessageNotificationBoxViewModel? ViewModel
    {
        get => DataContext as IPrivateMessageNotificationBoxViewModel;
        set => DataContext = value;
    }
}
