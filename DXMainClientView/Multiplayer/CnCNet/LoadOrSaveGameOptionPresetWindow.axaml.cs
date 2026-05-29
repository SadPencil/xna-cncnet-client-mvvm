using Avalonia.Controls;
using DXMainClientViewModel.Multiplayer.CnCNet;

namespace DXMainClientView.Multiplayer.CnCNet;

public partial class LoadOrSaveGameOptionPresetWindow : UserControl, ILoadOrSaveGameOptionPresetWindowView
{
    public LoadOrSaveGameOptionPresetWindow()
    {
        InitializeComponent();
    }

    public ILoadOrSaveGameOptionPresetWindowViewModel? ViewModel
    {
        get => DataContext as ILoadOrSaveGameOptionPresetWindowViewModel;
        set => DataContext = value;
    }
}
