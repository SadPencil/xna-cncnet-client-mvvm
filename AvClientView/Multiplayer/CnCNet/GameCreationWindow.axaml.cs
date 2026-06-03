using Avalonia.Controls;
using Avalonia.Data;

using AvClientMvvmContract.Multiplayer.CnCNet;

using Serilog;

namespace AvClientView.Multiplayer.CnCNet;

public partial class GameCreationWindow : UserControl, IGameCreationWindowView
{
    private bool _bindingsWired;

    public GameCreationWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        Log.Information("[LOG-View-GCW] OnDataContextChanged: new DC type={Type}",
            DataContext?.GetType().FullName ?? "null");

        if (!_bindingsWired && DataContext != null)
        {
            if (cmbTunnel != null)
            {
                cmbTunnel.Bind(ComboBox.ItemsSourceProperty, new Binding("TunnelNames"));
                cmbTunnel.Bind(ComboBox.SelectedIndexProperty, new Binding("SelectedTunnelIndex"));
            }
            if (cmbSkillLevel != null)
            {
                cmbSkillLevel.Bind(ComboBox.ItemsSourceProperty, new Binding("SkillLevelOptions"));
                cmbSkillLevel.Bind(ComboBox.SelectedIndexProperty, new Binding("SelectedSkillLevel"));
            }
            _bindingsWired = true;
            Log.Information("[LOG-View-GCW] ComboBox Bind() done on DataContext change");

            if (DataContext is IGameCreationWindowViewModel vm)
            {
                Log.Information("[LOG-View-GCW] VM state: TunnelNames.Count={T}, SkillLevelOptions.Count={S}, CanCreateGame={C}",
                    vm.TunnelNames?.Count ?? -1, vm.SkillLevelOptions?.Count ?? -1, vm.CanCreateGame);
                for (int i = 0; i < (vm.TunnelNames?.Count ?? 0) && i < 3; i++)
                    Log.Information("[LOG-View-GCW] TunnelNames[{Idx}]={Name}", i, vm.TunnelNames![i]);
            }
        }
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
