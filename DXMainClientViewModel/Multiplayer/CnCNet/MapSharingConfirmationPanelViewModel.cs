using System;
using System.Threading.Tasks;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

/// <summary>
/// ViewModel for the map sharing confirmation panel.
/// Contains all business logic from MapSharingConfirmationPanel.cs except XNA UI rendering.
/// </summary>
public partial class MapSharingConfirmationPanelViewModel : ObservableObject, IMapSharingConfirmationPanelViewModel // checked
{
    private readonly string MapSharingRequestText = ("The game host has selected a map that\ndoesn't exist on your local installation.").L10N("Client:Main:MapSharingRequestText");

    private readonly string MapSharingDownloadText = "Downloading map...".L10N("Client:Main:MapSharingDownloadText");

    private readonly string MapSharingFailedText = ("Downloading map failed. The game host\nneeds to change the map or you will be\nunable to participate in the match.").L10N("Client:Main:MapSharingFailedText");

    // --- Observable state ---

    [ObservableProperty]
    private string _mapName = string.Empty;

    [ObservableProperty]
    private string _hostName = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isDownloadAvailable;

    [ObservableProperty]
    private bool _isPanelVisible;

    // --- Events ---

    public event EventHandler? MapDownloadConfirmed;
    public event EventHandler? Cancelled;

    // --- Commands ---

    [RelayCommand]
    private async Task DownloadMap()
    {
        IsDownloadAvailable = false;
        StatusText = MapSharingDownloadText;
        MapDownloadConfirmed?.Invoke(this, EventArgs.Empty);

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Cancel()
    {
        IsPanelVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
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
        IsPanelVisible = true;
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
