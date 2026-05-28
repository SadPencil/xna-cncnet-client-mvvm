using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using ClientCore.Extensions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DXMainClientViewModel.Domain.Multiplayer.CnCNet;

using Rampastring.Tools;

namespace DXMainClientViewModel.Multiplayer.CnCNet;

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
    private string _descriptionText = string.Empty;

    [ObservableProperty]
    private int _selectedTunnelIndex = -1;

    [ObservableProperty]
    private string? _selectedTunnelName;

    [ObservableProperty]
    private bool _isConfirmEnabled;

    [ObservableProperty]
    private bool _isWindowVisible;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _tunnelNames = new();
    public IReadOnlyList<string> TunnelNames => _tunnelNames;

    // --- Events ---

    public event EventHandler<TunnelEventArgs>? TunnelSelected;
    public event EventHandler? Cancelled;

    // --- Constructor ---

    public TunnelSelectionWindowViewModel(TunnelHandler tunnelHandler)
    {
        this.tunnelHandler = tunnelHandler;

        tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
    }

    // --- Commands ---

    [RelayCommand]
    private void ConfirmSelection()
    {
        if (SelectedTunnelIndex < 0 || SelectedTunnelIndex >= tunnelHandler.Tunnels.Count)
            return;

        CnCNetTunnel tunnel = tunnelHandler.Tunnels[SelectedTunnelIndex];
        IsWindowVisible = false;
        TunnelSelected?.Invoke(this, new TunnelEventArgs(tunnel));
    }

    [RelayCommand]
    private void Cancel()
    {
        IsWindowVisible = false;
        Cancelled?.Invoke(this, EventArgs.Empty);
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
        IsWindowVisible = true;
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
