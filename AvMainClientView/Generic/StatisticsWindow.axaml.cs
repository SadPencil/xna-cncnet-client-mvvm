using AvMainClientMvvmContract.Generic;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientView.Generic;

public partial class StatisticsWindow : UserControl, IStatisticsWindowView
{
    private bool _showingTotalStats;

    public StatisticsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupTabButtons();
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("scoreviewerbg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "StatisticsWindow");
    }

    private void SetupTabButtons()
    {
        tabGameStatistics.Click += (_, _) => SwitchTab(false);
        tabTotalStatistics.Click += (_, _) => SwitchTab(true);
    }

    private void SwitchTab(bool showTotal)
    {
        _showingTotalStats = showTotal;
        panelGameStatistics.IsVisible = !showTotal;
        panelTotalStatistics.IsVisible = showTotal;
    }

    private void ApplyDefaultBackground(string texturePath)
    {
        try
        {
            var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
            if (iniOverlay == null) return;
            var fullPath = iniOverlay.FindTextureFile(texturePath);
            if (fullPath != null)
            {
                var bitmap = new Bitmap(fullPath);
                Background = new ImageBrush { Source = bitmap, Stretch = Stretch.UniformToFill };
            }
        }
        catch { }
    }

    public IStatisticsWindowViewModel? ViewModel
    {
        get => DataContext as IStatisticsWindowViewModel;
        set => DataContext = value;
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}
