using AvClientMvvmContract.Multiplayer.CnCNet;

using Avalonia.Controls;

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
