using System;
using System.Threading.Tasks;

using AvClientMvvmContract.Multiplayer.CnCNet;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the map sharing confirmation panel.
/// Contains all business logic from MapSharingConfirmationPanel.cs except XNA UI rendering.
/// </summary>
public partial class MapSharingConfirmationPanelViewModel : ObservableObject, IMapSharingConfirmationPanelViewModel
{
    private readonly string MapSharingRequestText = ("The game host has selected a map that\ndoesn't exist on your local installation.").L10N("Client:Main:MapSharingRequestText");

    private readonly string MapSharingDownloadText = "Downloading map...".L10N("Client:Main:MapSharingDownloadText");

    private readonly string MapSharingFailedText = ("Downloading map failed. The game host\nneeds to change the map or you will be\nunable to participate in the match.").L10N("Client:Main:MapSharingFailedText");

    // --- Observable state ---

    [ObservableProperty]
    public partial string MapName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HostName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsDownloadAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    private readonly Action? onMapDownloadConfirmed;
    private readonly Action? onCancelled;

    // --- Constructor ---

    public MapSharingConfirmationPanelViewModel(Action? onMapDownloadConfirmed = null, Action? onCancelled = null)
    {
        this.onMapDownloadConfirmed = onMapDownloadConfirmed;
        this.onCancelled = onCancelled;
    }

    // --- Commands ---

    [RelayCommand]
    private async Task DownloadMap()
    {
        IsDownloadAvailable = false;
        StatusText = MapSharingDownloadText;
        onMapDownloadConfirmed?.Invoke();

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
        onCancelled?.Invoke();
    }

    // --- Public methods ---

    /// <summary>
    /// Shows the panel for map download confirmation.
    /// </summary>
    public void ShowForMapDownload(string mapName, string hostName)
    {
        MapName = mapName;
        HostName = hostName;
        StatusText = MapSharingRequestText;
        IsDownloadAvailable = true;
        IsVisible = true;
    }

    /// <summary>
    /// Sets the status to indicate download is in progress.
    /// </summary>
    public void SetDownloadingStatus()
    {
        StatusText = MapSharingDownloadText;
        IsDownloadAvailable = false;
    }

    /// <summary>
    /// Sets the status to indicate download failed.
    /// </summary>
    public void SetFailedStatus()
    {
        StatusText = MapSharingFailedText;
        IsDownloadAvailable = false;
    }
}

