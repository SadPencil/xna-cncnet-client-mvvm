using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

namespace AvClientView.Multiplayer.CnCNet;

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
