using System.ComponentModel;
using System.IO;

using AvMainClientMvvmContract.Multiplayer.GameLobby;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using AvMainClientView.Services;

using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientView.Multiplayer.GameLobby;

public partial class SkirmishLobby : UserControl, ISkirmishLobbyView
{
    private IMapPreviewBoxViewModel? currentMapPreview;

    public SkirmishLobby()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ApplyDefaultBackground("gamelobbybg.png");

        var iniOverlay = ViewConstants.ServiceProvider.GetService<IIniLayoutOverlayService>();
        iniOverlay?.ApplyLayout(this, "SkirmishLobby");
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

    public IGameLobbyViewModel? ViewModel
    {
        get => DataContext as IGameLobbyViewModel;
        set
        {
            if (currentMapPreview != null)
                currentMapPreview.PropertyChanged -= OnMapPreviewPropertyChanged;

            DataContext = value;

            if (value?.MapPreviewBox != null)
            {
                currentMapPreview = value.MapPreviewBox;
                currentMapPreview.PropertyChanged += OnMapPreviewPropertyChanged;
                UpdateMapPreviewImage(currentMapPreview.MapPreviewImageBytes);
            }
        }
    }

    ISkirmishLobbyViewModel? ISkirmishLobbyView.ViewModel
    {
        get => DataContext as ISkirmishLobbyViewModel;
        set => ((ISkirmishLobbyView)this).ViewModel = value;
    }

    private void OnMapPreviewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IMapPreviewBoxViewModel.MapPreviewImageBytes)
            && sender is IMapPreviewBoxViewModel preview)
        {
            UpdateMapPreviewImage(preview.MapPreviewImageBytes);
        }
    }

    private void UpdateMapPreviewImage(byte[]? imageBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            mapPreviewImage.Source = null;
            return;
        }

        try
        {
            using var ms = new MemoryStream(imageBytes);
            mapPreviewImage.Source = new Bitmap(ms);
        }
        catch
        {
            mapPreviewImage.Source = null;
        }
    }

    void ISwitchableView.Show() => IsVisible = true;
    void ISwitchableView.Hide() => IsVisible = false;
    string ISwitchableView.GetDisplayName() => "Skirmish";
}
