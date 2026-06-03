using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

using Serilog;

namespace AvClientView.Multiplayer.CnCNet;

public partial class GameCreationWindow : UserControl, IGameCreationWindowView
{
    public GameCreationWindow()
    {
        InitializeComponent();
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set
        {
            DataContext = value;
            if (value != null)
            {
                Log.Information("[LOG-View-GCW] ViewModel set: TunnelNames.Count={T}, SkillLevelOptions.Count={S}, CanCreateGame={C}",
                    value.TunnelNames?.Count ?? -1,
                    value.SkillLevelOptions?.Count ?? -1,
                    value.CanCreateGame);
                if (value.TunnelNames != null)
                    for (int i = 0; i < value.TunnelNames.Count && i < 3; i++)
                        Log.Information("[LOG-View-GCW] TunnelNames[{I}]={Name}", i, value.TunnelNames[i]);
            }
        }
    }
}
