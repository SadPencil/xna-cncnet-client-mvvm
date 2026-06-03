using Avalonia.Controls;
using Avalonia.Data;

using AvClientMvvmContract.Multiplayer.CnCNet;

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
        // Wire ComboBoxes via code-behind Bind() because Avalonia ComboBox
        // ignores ItemTemplate when ItemsSource uses compiled {Binding} in AXAML.
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
    }

    public IGameCreationWindowViewModel? ViewModel
    {
        get => DataContext as IGameCreationWindowViewModel;
        set => DataContext = value;
    }
}
