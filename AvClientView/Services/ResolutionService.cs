using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using AvClientMvvmContract.ViewServices;

using Serilog;

namespace AvClientView.Services;

/// <summary>
/// Provides the primary monitor's desktop resolution and supported display modes
/// using OS-level APIs (xrandr on Linux, EnumDisplaySettings on Windows).
/// </summary>
public class ResolutionService : IResolutionService
{
    private int? _desktopWidth;
    private int? _desktopHeight;
    private IReadOnlyList<(int Width, int Height)>? _displayModes;

    public int DesktopWidth => _desktopWidth ??= QueryPrimaryScreenBounds().width;
    public int DesktopHeight => _desktopHeight ??= QueryPrimaryScreenBounds().height;

    public IReadOnlyList<(int Width, int Height)> GetSupportedDisplayModes()
    {
        if (_displayModes != null)
            return _displayModes;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            _displayModes = QueryXrandrDisplayModes();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _displayModes = QueryWindowsDisplayModes();

        // Fall back to the desktop resolution only
        if (_displayModes == null || _displayModes.Count == 0)
            _displayModes = [(DesktopWidth, DesktopHeight)];

        return _displayModes;
    }

    // ----------------------------------------------------------------
    // Desktop resolution via Avalonia
    // ----------------------------------------------------------------

    private static (int width, int height) QueryPrimaryScreenBounds()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Screens is { } screens)
        {
            var primary = screens.Primary;
            if (primary != null)
                return (primary.Bounds.Width, primary.Bounds.Height);

            if (screens.All.Count > 0)
            {
                var first = screens.All[0];
                return (first.Bounds.Width, first.Bounds.Height);
            }
        }

        return (1920, 1080);
    }

    // ----------------------------------------------------------------
    // Linux: parse xrandr output
    // ----------------------------------------------------------------

    private static IReadOnlyList<(int Width, int Height)> QueryXrandrDisplayModes()
    {
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
                Log.Warning("[ResolutionService] xrandr exited with code {ExitCode}", process.ExitCode);
                return [];
            }

            return ParseXrandrOutput(output);
        }
        catch (Exception ex)
        {
            Log.Warning("[ResolutionService] Failed to query xrandr: {Message}", ex.Message);
            return [];
        }
    }

    /// <summary>
    /// Parses xrandr --query output for display mode lines.
    /// Each connected output lists modes like:
    ///    1920x1080     60.05*+  59.97    59.96
    ///    1680x1050     59.95    59.88
    /// We extract unique WidthxHeight pairs across all connected outputs.
    /// </summary>
    private static IReadOnlyList<(int Width, int Height)> ParseXrandrOutput(string output)
    {
        var modes = new HashSet<(int, int)>();
        bool inConnectedOutput = false;

        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.TrimStart();

            // Lines that mark a new output section start with the output name
            // e.g. "eDP-1 connected ..." or "HDMI-1 disconnected ..."
            if (!trimmed.StartsWith(" ") && trimmed.Contains(' '))
            {
                string[] parts = trimmed.Split(' ', 2);
                inConnectedOutput = parts.Length >= 2 && parts[1].StartsWith("connected");
                continue;
            }

            // Mode lines are indented and match "WIDTHxHEIGHT"
            if (inConnectedOutput && trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                int xIndex = trimmed.IndexOf('x');
                if (xIndex <= 0) continue;

                // Scan digits before 'x'
                int wStart = 0;
                while (wStart < xIndex && char.IsDigit(trimmed[wStart]))
                    wStart++;
                string widthStr = trimmed[..xIndex];

                // Scan digits after 'x'
                int hEnd = xIndex + 1;
                while (hEnd < trimmed.Length && char.IsDigit(trimmed[hEnd]))
                    hEnd++;
                string heightStr = trimmed.Substring(xIndex + 1, hEnd - xIndex - 1);

                if (int.TryParse(widthStr, out int w) && int.TryParse(heightStr, out int h))
                    modes.Add((w, h));
            }
        }

        return modes.OrderBy(m => m.Item1 * m.Item2).ThenBy(m => m.Item1).ToList();
    }

    // ----------------------------------------------------------------
    // Windows: EnumDisplaySettings
    // ----------------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(
        string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    private const int ENUM_CURRENT_SETTINGS = -1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    private static IReadOnlyList<(int Width, int Height)> QueryWindowsDisplayModes()
    {
        var modes = new HashSet<(int, int)>();
        int i = 0;
        DEVMODE devMode = default;
        devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();

        while (EnumDisplaySettings(null, i, ref devMode))
        {
            if (devMode.dmPelsWidth > 0 && devMode.dmPelsHeight > 0)
                modes.Add((devMode.dmPelsWidth, devMode.dmPelsHeight));
            i++;
        }

        return modes.OrderBy(m => m.Item1 * m.Item2).ThenBy(m => m.Item1).ToList();
    }
}
