using AvMainClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

namespace AvMainClientView.Multiplayer.CnCNet;

public partial class ChoiceNotificationBox : UserControl, IChoiceNotificationBoxView
{
    public ChoiceNotificationBox()
    {
        InitializeComponent();
    }

    public IChoiceNotificationBoxViewModel? ViewModel
    {
        get => DataContext as IChoiceNotificationBoxViewModel;
        set => DataContext = value;
    }
}
