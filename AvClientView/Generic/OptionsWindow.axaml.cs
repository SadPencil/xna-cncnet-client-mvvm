using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvClientMvvmContract.Generic;

using AvClientView.Services;

namespace AvClientView.Generic;

public partial class OptionsWindow : UserControl, IOptionsWindowView
{
    public IIniLayoutOverlayService? IniOverlayService { get; set; }

    public OptionsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("optionsbg.png");

        IniOverlayService?.ApplyLayout(this, "OptionsWindow");

        SetupPanelDataContexts();
    }

    private void SetupPanelDataContexts()
    {
        var vm = ViewModel;
        if (vm == null) return;

        if (displayPanel != null)    displayPanel.DataContext = vm.DisplayOptions;
        if (audioPanel != null)      audioPanel.DataContext = vm.AudioOptions;
        if (gamePanel != null)       gamePanel.DataContext = vm.GameOptions;
        if (cncnetPanel != null)     cncnetPanel.DataContext = vm.CnCNetOptions;
        if (updaterPanel != null)    updaterPanel.DataContext = vm.UpdaterOptions;
        if (componentsPanel != null) componentsPanel.DataContext = vm.ComponentsOptions;
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            if (IniOverlayService == null) return;
            var fullPath = IniOverlayService.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    public IOptionsWindowViewModel? ViewModel
    {
        get => DataContext as IOptionsWindowViewModel;
        set => DataContext = value;
    }
}
