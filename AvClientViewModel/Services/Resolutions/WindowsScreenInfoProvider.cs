using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AvClientViewModel.Services.Resolutions;

/// <summary>
/// Queries display modes and current desktop resolution via EnumDisplaySettings on Windows.
/// </summary>
internal sealed class WindowsScreenInfoProvider : IScreenInfoProvider
{
    private bool _queried;
    private int _desktopWidth;
    private int _desktopHeight;
    private IReadOnlyList<(int Width, int Height)> _modes = [];

    public int Priority => 0;

    public bool IsApplicable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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
            // Get current desktop resolution
            _desktopWidth = GetSystemMetrics(SM_CXSCREEN);
            _desktopHeight = GetSystemMetrics(SM_CYSCREEN);

            // Enumerate all supported display modes
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

            if (modes.Count > 0)
                _modes = modes.OrderBy(m => m.Item1 * m.Item2).ThenBy(m => m.Item1).ToList();
        }
        catch
        {
            // Keep defaults
        }
    }

    // ----------------------------------------------------------------
    // P/Invoke
    // ----------------------------------------------------------------

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(
        string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

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
}
