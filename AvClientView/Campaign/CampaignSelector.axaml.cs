using Avalonia.Controls;

using AvClientMvvmContract.Campaign;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Campaign;

public partial class CampaignSelector : UserControl
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public CampaignSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "MainMenu/dbak.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "CampaignSelector");
    }

    public ICampaignSelectorViewModel? ViewModel
    {
        get => DataContext as ICampaignSelectorViewModel;
        set => DataContext = value;
    }
}
