using Avalonia.Controls;

using AvClientMvvmContract.Campaign;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Campaign;

public partial class CampaignSelector : UserControl
{
    private IIniLayoutOverlayService? _iniOverlayService;
    private bool _layoutApplied;

    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService
    {
        get => _iniOverlayService;
        set
        {
            _iniOverlayService = value;
            if (value != null && !_layoutApplied)
                ApplyLayoutAndTheme(value);
        }
    }

    public CampaignSelector()
    {
        InitializeComponent();
    }

    private void ApplyLayoutAndTheme(IIniLayoutOverlayService iniOverlay)
    {
        _layoutApplied = true;

        // Set default background matching old XNA client (AssetLoader.LoadTexture("missionselectorbg.png"))
        BackgroundHelper.ApplyDefaultBackground(this, "missionselectorbg.png", iniOverlay);

        // Apply INI layout which may override the background with BackgroundTexture=cncnetlobbybg.png
        iniOverlay.ApplyLayout(this, "CampaignSelector");
    }

    public ICampaignSelectorViewModel? ViewModel
    {
        get => DataContext as ICampaignSelectorViewModel;
        set => DataContext = value;
    }
}
