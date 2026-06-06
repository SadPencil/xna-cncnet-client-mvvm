using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using AvClientMvvmContract.Multiplayer.CnCNet;

using AvClientViewModel.Domain.Multiplayer.CnCNet;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Rampastring.Tools;

namespace AvClientViewModel.Multiplayer.CnCNet;


/// <summary>
/// ViewModel for the tunnel selection window.
/// Contains all business logic from TunnelSelectionWindow.cs except XNA UI rendering.
/// </summary>
public partial class TunnelSelectionWindowViewModel : ObservableObject, ITunnelSelectionWindowViewModel
{
    private readonly TunnelHandler tunnelHandler;

    private string? originalTunnelAddress;

    // --- Observable state ---

    [ObservableProperty]
    public partial string DescriptionText { get; set; }  = string.Empty;

    [ObservableProperty]
    public partial int SelectedTunnelIndex { get; set; }  = -1;

    [ObservableProperty]
    public partial string? SelectedTunnelName { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    // --- Observable collections ---

    private readonly ObservableCollection<string> _tunnelNames = new();
    public IReadOnlyList<string> TunnelNames => _tunnelNames;

    private readonly Action<CnCNetTunnel>? onTunnelSelected;
    private readonly Action? onCancelled;

    // --- Constructor ---

    public TunnelSelectionWindowViewModel(TunnelHandler tunnelHandler, Action<CnCNetTunnel>? onTunnelSelected = null, Action? onCancelled = null)
    {
        this.tunnelHandler = tunnelHandler;
        this.onTunnelSelected = onTunnelSelected;
        this.onCancelled = onCancelled;

        tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
    }

    // --- Commands ---

    [RelayCommand]
    private void ConfirmSelection()
    {
        if (SelectedTunnelIndex < 0 || SelectedTunnelIndex >= tunnelHandler.Tunnels.Count)
            return;

        CnCNetTunnel tunnel = tunnelHandler.Tunnels[SelectedTunnelIndex];
        IsVisible = false;
        onTunnelSelected?.Invoke(tunnel);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsVisible = false;
        onCancelled?.Invoke();
    }

    [RelayCommand]
    private void RefreshTunnels()
    {
        // TunnelHandler manages its own refresh cycle.
        // This command is available for the View to trigger if needed.
    }

    // --- Public methods ---

    /// <summary>
    /// Opens the tunnel selection window with the specified description
    /// and optionally pre-selects a tunnel by address.
    /// </summary>
    public void Open(string description, string? tunnelAddress = null)
    {
        DescriptionText = description;
        originalTunnelAddress = tunnelAddress;

        RefreshTunnelList();

        if (!string.IsNullOrWhiteSpace(tunnelAddress))
        {
            int index = tunnelHandler.Tunnels.FindIndex(t => t.Address == tunnelAddress);
            SelectedTunnelIndex = index >= 0 ? index : -1;
        }
        else
        {
            SelectedTunnelIndex = -1;
        }

        IsConfirmEnabled = false;
        IsVisible = true;
    }

    // --- Property change handlers ---

    partial void OnSelectedTunnelIndexChanged(int value)
    {
        if (value >= 0 && value < tunnelHandler.Tunnels.Count)
        {
            SelectedTunnelName = tunnelHandler.Tunnels[value].Name;
            IsConfirmEnabled = tunnelHandler.Tunnels[value].Address != originalTunnelAddress;
        }
        else
        {
            SelectedTunnelName = null;
            IsConfirmEnabled = false;
        }
    }

    // --- Helpers ---

    private void TunnelHandler_TunnelsRefreshed(object? sender, EventArgs e)
    {
        RefreshTunnelList();
    }

    private void RefreshTunnelList()
    {
        _tunnelNames.Clear();
        foreach (var tunnel in tunnelHandler.Tunnels)
        {
            string pingText = tunnel.PingInMs < 0
                ? "Unknown".L10N("Client:Main:UnknownPing")
                : tunnel.PingInMs + " ms";

            _tunnelNames.Add($"{tunnel.Name} | {pingText} | {tunnel.Clients}/{tunnel.MaxClients}");
        }
    }
}


