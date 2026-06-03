using Avalonia.Controls;
using Avalonia.Data;

using AvClientMvvmContract.Multiplayer.CnCNet;

using Serilog;

namespace AvClientView.Multiplayer.CnCNet;

public partial class GameCreationWindow : UserControl, IGameCreationWindowView
{
    public GameCreationWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Log.Information("[LOG-View-GCW] OnLoaded: cmbTunnel={T}, cmbSkillLevel={S}",
            cmbTunnel != null ? "present" : "null",
            cmbSkillLevel != null ? "present" : "null");

        // Wire ComboBoxes via code-behind Bind() because Avalonia ComboBox
        // ignores ItemTemplate when ItemsSource uses compiled {Binding} in AXAML.
        if (cmbTunnel != null)
        {
            cmbTunnel.Bind(ComboBox.ItemsSourceProperty, new Binding("TunnelNames"));
            cmbTunnel.Bind(ComboBox.SelectedIndexProperty, new Binding("SelectedTunnelIndex"));
            Log.Information("[LOG-View-GCW] cmbTunnel Bind() done");
        }
        if (cmbSkillLevel != null)
        {
            cmbSkillLevel.Bind(ComboBox.ItemsSourceProperty, new Binding("SkillLevelOptions"));
            cmbSkillLevel.Bind(ComboBox.SelectedIndexProperty, new Binding("SelectedSkillLevel"));
            Log.Information("[LOG-View-GCW] cmbSkillLevel Bind() done");
        }

        // Log VM state
        var vm = DataContext as IGameCreationWindowViewModel;
        if (vm != null)
        {
            Log.Information("[LOG-View-GCW] VM: TunnelNames.Count={T}, SkillLevelOptions.Count={S}, CanCreateGame={C}",
                vm.TunnelNames?.Count ?? -1,
                vm.SkillLevelOptions?.Count ?? -1,
                vm.CanCreateGame);
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
            DataContext = value;
            Log.Information("[LOG-View-GCW] ViewModel set: type={Type}", value?.GetType().FullName ?? "null");
        }
    }
}
