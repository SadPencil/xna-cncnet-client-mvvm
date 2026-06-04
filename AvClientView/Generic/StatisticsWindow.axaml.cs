using Avalonia.Controls;

using AvClientMvvmContract.Generic;

using AvClientView.Controls;
using AvClientView.Services;


namespace AvClientView.Generic;

public partial class StatisticsWindow : UserControl, IStatisticsWindowView
{
    public AvClientView.Services.IIniLayoutOverlayService? IniOverlayService { get; set; }

    public StatisticsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        BackgroundHelper.ApplyDefaultBackground(this, "scoreviewerbg.png", IniOverlayService);

        var iniOverlay = IniOverlayService;
        iniOverlay?.ApplyLayout(this, "StatisticsWindow");
    }

    public IStatisticsWindowViewModel? ViewModel
    {
        get => DataContext as IStatisticsWindowViewModel;
        set => DataContext = value;
    }

    public void Show() => IsVisible = true;
    public void Hide() => IsVisible = false;
}
