using Avalonia;
using Avalonia.Controls;

using AvClientMvvmContract.Multiplayer.CnCNet;

using Serilog;

namespace AvClientView.Multiplayer.CnCNet;

public partial class GameCreationWindow : UserControl, IGameCreationWindowView
{
    public GameCreationWindow()
    {
        InitializeComponent();
        Log.Information("[LOG-View-GCW] Constructor called");
        PropertyChanged += (s, e) =>
        {
            if (e.Property == DataContextProperty)
                Log.Information("[LOG-View-GCW] PropertyChanged DataContext: new={New}, old={Old}",
                    e.NewValue?.GetType().FullName ?? "null",
                    e.OldValue?.GetType().FullName ?? "null");
        };
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        Log.Information("[LOG-View-GCW] OnDataContextChanged: DataContext type={Type}",
            DataContext?.GetType().FullName ?? "null");
        LogVmState();
    }

    private void LogVmState()
    {
        if (DataContext is IGameCreationWindowViewModel vm)
        {
            Log.Information("[LOG-View-GCW] VM state: TunnelNames.Count={T}, SkillLevelOptions.Count={S}, CanCreateGame={C}",
                vm.TunnelNames?.Count ?? -1, vm.SkillLevelOptions?.Count ?? -1, vm.CanCreateGame);
            if (vm.TunnelNames != null)
                for (int i = 0; i < vm.TunnelNames.Count && i < 3; i++)
                    Log.Information("[LOG-View-GCW] TunnelNames[{Idx}]={Name}", i, vm.TunnelNames[i]);
        }
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set
        {
            Log.Information("[LOG-View-GCW] ViewModel setter called, type={Type}",
                value?.GetType().FullName ?? "null");
            DataContext = value;
        }
    }
}
