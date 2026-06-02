using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

using Serilog;

namespace AvClientViewModel.Domain.Multiplayer.CnCNet
{
    public class TunnelHandler : IDisposable
    {
        /// <summary>
        /// Determines the time between pinging the current tunnel (if it's set).
        /// </summary>
        private const double CURRENT_TUNNEL_PING_INTERVAL = 20.0;

        /// <summary>
        /// A reciprocal to the value which determines how frequent the full tunnel
        /// refresh would be done instead of just pinging the current tunnel (1/N of
        /// current tunnel ping refreshes would be substituted by a full list refresh).
        /// Multiply by <see cref="CURRENT_TUNNEL_PING_INTERVAL"/> to get the interval
        /// between full list refreshes.
        /// </summary>
        private const uint CYCLES_PER_TUNNEL_LIST_REFRESH = 6;

        private const int SUPPORTED_TUNNEL_VERSION = 2;
        private static readonly TimeSpan tunnelRefreshInterval = TimeSpan.FromSeconds(CURRENT_TUNNEL_PING_INTERVAL);

        private readonly object _refreshLock = new object();
        private bool _refreshInProgress = false;
        private Timer refreshTimer;
        private bool disposed = false;

        public TunnelHandler()
        {
            refreshTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        public List<CnCNetTunnel> Tunnels { get; private set; } = new List<CnCNetTunnel>();
        public CnCNetTunnel CurrentTunnel { get; set; } = null;

        public event EventHandler TunnelsRefreshed;
        public event EventHandler CurrentTunnelPinged;
        public event Action<int> TunnelPinged;

        private uint skipCount = 0;

        public void Start()
        {
            refreshTimer.Change(TimeSpan.Zero, tunnelRefreshInterval);
        }

        public void Stop()
        {
            refreshTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnTimerTick(object state)
        {
            if (skipCount % CYCLES_PER_TUNNEL_LIST_REFRESH == 0)
            {
                skipCount = 0;
                RefreshTunnelsAsync();
            }
            else if (CurrentTunnel != null)
            {
                _ = PingCurrentTunnelAsync(true);
            }

            skipCount++;
        }

        private void RefreshTunnelsAsync()
        {
            lock (_refreshLock)
            {
                if (_refreshInProgress)
                    return;
                _refreshInProgress = true;
            }

            Task.Run(() =>
            {
                try
                {
                    List<CnCNetTunnel> tunnels = RefreshTunnels();
                    HandleRefreshedTunnels(tunnels);
                }
                finally
                {
                    lock (_refreshLock)
                    {
                        _refreshInProgress = false;
                    }
                }
            });
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> newTunnels)
        {
            if (newTunnels.Count == 0)
            {
                TunnelsRefreshed?.Invoke(this, EventArgs.Empty);
                return;
            }

            var existingTunnels = Tunnels.ToDictionary(t => $"{t.Address}:{t.Port}");
            var updatedTunnels = new List<CnCNetTunnel>();

            foreach (var newTunnel in newTunnels)
            {
                string key = $"{newTunnel.Address}:{newTunnel.Port}";
                if (existingTunnels.TryGetValue(key, out var existingTunnel))
                {
                    existingTunnel.UpdateFrom(newTunnel);
                    updatedTunnels.Add(existingTunnel);
                }
                else
                {
                    updatedTunnels.Add(newTunnel);
                }
            }

            Tunnels = updatedTunnels;
            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < Tunnels.Count; i++)
            {
                if (UserINISettings.Instance.PingUnofficialCnCNetTunnels || Tunnels[i].Official || Tunnels[i].Recommended)
                    _ = PingListTunnelAsync(i);
            }

            if (CurrentTunnel != null)
            {
                var updatedTunnel = Tunnels.Find(t => t.Address == CurrentTunnel.Address && t.Port == CurrentTunnel.Port);
                if (updatedTunnel != null)
                {
                    CurrentTunnel = updatedTunnel;
                    CurrentTunnelPinged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    PingCurrentTunnelAsync();
                }
            }
        }

        private Task PingListTunnelAsync(int index)
        {
            return Task.Run(() =>
            {
                Tunnels[index].UpdatePing();
                TunnelPinged?.Invoke(index);
            });
        }

        private Task PingCurrentTunnelAsync(bool checkTunnelList = false)
        {
            return Task.Run(() =>
            {
                var tunnel = CurrentTunnel;
                if (tunnel == null) return;

                tunnel.UpdatePing();
                CurrentTunnelPinged?.Invoke(this, EventArgs.Empty);

                if (checkTunnelList)
                {
                    int tunnelIndex = Tunnels.FindIndex(t => t.Address == tunnel.Address && t.Port == tunnel.Port);
                    if (tunnelIndex > -1)
                        TunnelPinged?.Invoke(tunnelIndex);
                }
            });
        }

        private bool OnlineTunnelDataAvailable => !string.IsNullOrWhiteSpace(ClientConfiguration.Instance.CnCNetTunnelListURL);
        private bool OfflineTunnelDataAvailable => SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache").Exists;

        private byte[] GetRawTunnelDataOnline()
        {
            return new TimedHttpClient(10000).GetBytes(ClientConfiguration.Instance.CnCNetTunnelListURL);
        }

        private byte[] GetRawTunnelDataOffline()
        {
            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");
            return File.ReadAllBytes(tunnelCacheFile.FullName);
        }

        private byte[] GetRawTunnelData(int retryCount = 2)
        {
            Log.Information("Fetching tunnel server info.");

            if (OnlineTunnelDataAvailable)
            {
                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        byte[] data = GetRawTunnelDataOnline();
                        return data;
                    }
                    catch (Exception ex)
                    {
                        Log.Information("Error when downloading tunnel server info: " + ex.Message);
                        if (i < retryCount - 1)
                            Log.Information("Retrying.");
                        else
                            Log.Information("Fetching tunnel server list failed.");
                    }
                }
            }
            else
            {
                Log.Information("Fetching tunnel server list online is disabled.");
            }

            if (OfflineTunnelDataAvailable)
            {
                Log.Information("Using cached tunnel data.");
                byte[] data = GetRawTunnelDataOffline();
                return data;
            }
            else
                Log.Information("Tunnel cache file doesn't exist!");

            return null;
        }

        private List<CnCNetTunnel> RefreshTunnels()
        {
            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();
            var seenAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");

            byte[] data = GetRawTunnelData();
            if (data is null)
                return returnValue;

            string convertedData = Encoding.Default.GetString(data);

            string[] serverList = convertedData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string serverInfo in serverList.Skip(1))
            {
                try
                {
                    CnCNetTunnel tunnel = CnCNetTunnel.Parse(serverInfo);

                    if (tunnel == null)
                        continue;

                    if (tunnel.RequiresPassword)
                        continue;

                    if (tunnel.Version != SUPPORTED_TUNNEL_VERSION)
                        continue;

                    if (!seenAddresses.Add($"{tunnel.Address}:{tunnel.Port}"))
                        continue;

                    returnValue.Add(tunnel);
                }
                catch (Exception ex)
                {
                    Log.Information("Caught an exception when parsing a tunnel server: " + ex.ToString());
                }
            }

            if (returnValue.Count > 0)
            {
                try
                {
                    if (tunnelCacheFile.Exists)
                        tunnelCacheFile.Delete();

                    DirectoryInfo clientDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                    if (!clientDirectoryInfo.Exists)
                        clientDirectoryInfo.Create();

                    File.WriteAllBytes(tunnelCacheFile.FullName, data);
                }
                catch (Exception ex)
                {
                    Log.Information("Refreshing tunnel cache file failed! Returned error: " + ex.ToString());
                }
            }

            Log.Information($"Successfully refreshed tunnel cache with {returnValue.Count} servers.");
            return returnValue;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                refreshTimer?.Dispose();
                disposed = true;
            }
        }
    }
}
