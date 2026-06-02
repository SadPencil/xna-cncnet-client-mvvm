using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvClientView.Multiplayer.CnCNet;

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
