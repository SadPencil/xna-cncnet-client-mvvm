using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

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
