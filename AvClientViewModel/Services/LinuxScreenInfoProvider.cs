using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Serilog;

namespace AvClientViewModel.Services;

/// <summary>
/// Queries display modes and current desktop resolution via xrandr on Linux.
/// </summary>
internal sealed class LinuxScreenInfoProvider : IScreenInfoProvider
{
    private bool _queried;
    private int _desktopWidth = 1920;
    private int _desktopHeight = 1080;
    private IReadOnlyList<(int Width, int Height)> _modes = [(1920, 1080)];

    public int Priority => 0;

    public bool IsApplicable
    {
        get
        {
            try
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "xrandr",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                proc?.WaitForExit(2000);
                return proc?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public int DesktopWidth { get { EnsureQueried(); return _desktopWidth; } }
    public int DesktopHeight { get { EnsureQueried(); return _desktopHeight; } }

    public IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes()
    {
        EnsureQueried();
        return _modes;
    }

    private void EnsureQueried()
    {
        if (_queried) return;
        _queried = true;

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xrandr",
                    Arguments = "--query",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            if (process.ExitCode != 0)
            {
                Log.Warning("[LinuxScreenInfoProvider] xrandr exit code {ExitCode}", process.ExitCode);
                return;
            }

            ParseXrandr(output);
        }
        catch (Exception ex)
        {
            Log.Warning("[LinuxScreenInfoProvider] xrandr failed: {Message}", ex.Message);
        }
    }

    private void ParseXrandr(string output)
    {
        var modes = new HashSet<(int, int)>();
        bool inConnectedOutput = false;
        bool foundCurrent = false;

        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.TrimStart();

            if (!trimmed.StartsWith(" ") && trimmed.Contains(' '))
            {
                string[] parts = trimmed.Split(' ', 2);
                inConnectedOutput = parts.Length >= 2 && parts[1].StartsWith("connected");
                continue;
            }

            if (inConnectedOutput && trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                int spaceIdx = trimmed.IndexOf(' ');
                if (spaceIdx < 0) continue;

                string resPart = trimmed[..spaceIdx];
                string flagsPart = trimmed[spaceIdx..].TrimStart();

                int xIdx = resPart.IndexOf('x');
                if (xIdx <= 0) continue;

                if (int.TryParse(resPart[..xIdx], out int w)
                    && int.TryParse(resPart[(xIdx + 1)..], out int h))
                {
                    modes.Add((w, h));

                    // The current mode has a '*' in the flags column
                    if (!foundCurrent && flagsPart.Contains('*'))
                    {
                        _desktopWidth = w;
                        _desktopHeight = h;
                        foundCurrent = true;
                    }
                }
            }
        }

        if (modes.Count > 0)
            _modes = modes.OrderBy(m => m.Item1 * m.Item2).ThenBy(m => m.Item1).ToList();
    }
}
