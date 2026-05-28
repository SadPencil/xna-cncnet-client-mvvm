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
/// ViewModel for the tunnel list box.
/// Contains all business logic from TunnelListBox.cs except XNA UI rendering.
/// </summary>
public partial class TunnelListBoxViewModel : ObservableObject, ITunnelListBoxViewModel // checked
{
    private readonly TunnelHandler tunnelHandler;

    private int bestTunnelIndex;
    private int lowestTunnelRating = int.MaxValue;
    private bool isManuallySelectedTunnel;
    private string? manuallySelectedTunnelAddress;

    // --- Observable state ---

    [ObservableProperty]
    private int _selectedTunnelIndex;

    [ObservableProperty]
    private string? _selectedTunnelName;

    // --- Observable collections ---

    private readonly ObservableCollection<string> _tunnelNames = new();
    public IReadOnlyList<string> TunnelNames => _tunnelNames;

    private readonly ObservableCollection<string> _tunnelAddresses = new();
    public IReadOnlyList<string> TunnelAddresses => _tunnelAddresses;

    // --- Events ---

    public event EventHandler? ListRefreshed;

    // --- Constructor ---

    public TunnelListBoxViewModel(TunnelHandler tunnelHandler)
    {
        this.tunnelHandler = tunnelHandler;

        tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
        tunnelHandler.TunnelPinged += TunnelHandler_TunnelPinged;
    }

    // --- Commands ---

    [RelayCommand]
    private void RefreshTunnels()
    {
        // TunnelHandler manages its own refresh cycle via Start().
        // This command is a no-op; tunnel list updates arrive via TunnelsRefreshed event.
    }

    [RelayCommand]
    private void SelectTunnel()
    {
        if (SelectedTunnelIndex >= 0 && SelectedTunnelIndex < tunnelHandler.Tunnels.Count)
        {
            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = tunnelHandler.Tunnels[SelectedTunnelIndex].Address;
        }
    }

    // --- Public methods ---

    /// <summary>
    /// Selects a tunnel from the list with the given address.
    /// </summary>
    public void SelectTunnelByAddress(string address)
    {
        int index = tunnelHandler.Tunnels.FindIndex(t => t.Address == address);
        if (index > -1)
        {
            SelectedTunnelIndex = index;
            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = address;
        }
    }

    /// <summary>
    /// Gets whether a tunnel with the given address is selected.
    /// </summary>
    public bool IsTunnelSelected(string address) =>
        tunnelHandler.Tunnels.FindIndex(t => t.Address == address) == SelectedTunnelIndex;

    // --- Property change handlers ---

    partial void OnSelectedTunnelIndexChanged(int value)
    {
        if (value >= 0 && value < tunnelHandler.Tunnels.Count)
        {
            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = tunnelHandler.Tunnels[value].Address;
            SelectedTunnelName = tunnelHandler.Tunnels[value].Name;
        }
    }

    // --- Helpers ---

    private void TunnelHandler_TunnelsRefreshed(object? sender, EventArgs e)
    {
        _tunnelNames.Clear();
        _tunnelAddresses.Clear();

        bestTunnelIndex = 0;
        lowestTunnelRating = int.MaxValue;

        for (int i = 0; i < tunnelHandler.Tunnels.Count; i++)
        {
            CnCNetTunnel tunnel = tunnelHandler.Tunnels[i];

            string pingText = tunnel.PingInMs < 0
                ? "Unknown".L10N("Client:Main:UnknownPing")
                : tunnel.PingInMs + " ms";

            string displayText = $"{tunnel.Name} | {Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO)} | {pingText} | {tunnel.Clients}/{tunnel.MaxClients}";

            _tunnelNames.Add(displayText);
            _tunnelAddresses.Add(tunnel.Address);

            if ((tunnel.Official || tunnel.Recommended) && tunnel.PingInMs > -1)
            {
                int rating = GetTunnelRating(tunnel);
                if (rating < lowestTunnelRating)
                {
                    bestTunnelIndex = i;
                    lowestTunnelRating = rating;
                }
            }
        }

        if (tunnelHandler.Tunnels.Count > 0)
        {
            if (!isManuallySelectedTunnel)
            {
                SelectedTunnelIndex = bestTunnelIndex;
            }
            else
            {
                int manuallySelectedIndex = tunnelHandler.Tunnels.FindIndex(t => t.Address == manuallySelectedTunnelAddress);

                if (manuallySelectedIndex == -1)
                {
                    SelectedTunnelIndex = bestTunnelIndex;
                    isManuallySelectedTunnel = false;
                }
                else
                {
                    SelectedTunnelIndex = manuallySelectedIndex;
                }
            }
        }

        ListRefreshed?.Invoke(this, EventArgs.Empty);
    }

    private void TunnelHandler_TunnelPinged(int tunnelIndex)
    {
        if (tunnelIndex < 0 || tunnelIndex >= tunnelHandler.Tunnels.Count)
            return;

        CnCNetTunnel tunnel = tunnelHandler.Tunnels[tunnelIndex];

        string pingText = tunnel.PingInMs == -1
            ? "Unknown".L10N("Client:Main:UnknownPing")
            : tunnel.PingInMs + " ms";

        string displayText = $"{tunnel.Name} | {Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO)} | {pingText} | {tunnel.Clients}/{tunnel.MaxClients}";

        if (tunnelIndex < _tunnelNames.Count)
            _tunnelNames[tunnelIndex] = displayText;

        if (tunnel.PingInMs != -1)
        {
            int rating = GetTunnelRating(tunnel);

            if (!isManuallySelectedTunnel && (tunnel.Recommended || tunnel.Official) && rating < lowestTunnelRating)
            {
                bestTunnelIndex = tunnelIndex;
                lowestTunnelRating = rating;
                SelectedTunnelIndex = tunnelIndex;
            }
        }
    }

    private static int GetTunnelRating(CnCNetTunnel tunnel)
    {
        double usageRatio = (double)tunnel.Clients / tunnel.MaxClients;

        if (usageRatio == 0)
            usageRatio = 0.1;

        usageRatio *= 100.0;

        return Convert.ToInt32(Math.Pow(tunnel.PingInMs, 2.0) * usageRatio);
    }
}
