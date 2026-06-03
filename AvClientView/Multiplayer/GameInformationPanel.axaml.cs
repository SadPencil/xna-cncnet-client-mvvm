using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer;

namespace AvClientView.Multiplayer;

public partial class GameInformationPanel : UserControl, IGameInformationPanelView
{
    public GameInformationPanel()
    {
        InitializeComponent();
    }

    public IGameInformationPanelViewModel? ViewModel
    {
        get => DataContext as IGameInformationPanelViewModel;
        set => DataContext = value;
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}
