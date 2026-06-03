using Avalonia;
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
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != DataContextProperty) return;
        Log.Information("[LOG-View-GCW] DataContext changed: new type={Type}",
            DataContext?.GetType().FullName ?? "null");

        if (!_bindingsWired && DataContext != null)
        {
            WireBindings();
        }
    }

    private void WireBindings()
    {
        Log.Information("[LOG-View-GCW] WireBindings: cmbTunnel={T}, cmbSkillLevel={S}",
            cmbTunnel != null, cmbSkillLevel != null);

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
        Log.Information("[LOG-View-GCW] ComboBox Bind() done");

        if (DataContext is IGameCreationWindowViewModel vm)
        {
            Log.Information("[LOG-View-GCW] VM: TunnelNames.Count={T}, SkillLevelOptions={S}, CanCreateGame={C}",
                vm.TunnelNames?.Count ?? -1, vm.SkillLevelOptions?.Count ?? -1, vm.CanCreateGame);
            for (int i = 0; i < (vm.TunnelNames?.Count ?? 0) && i < 3; i++)
                Log.Information("[LOG-View-GCW] TunnelNames[{I}]={Name}", i, vm.TunnelNames![i]);
        }
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
