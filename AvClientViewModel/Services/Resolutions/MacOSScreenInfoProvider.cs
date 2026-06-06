using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AvClientViewModel.Services.Resolutions;

/// <summary>
/// Queries display modes and current desktop resolution via CoreGraphics on macOS.
/// </summary>
internal sealed class MacOSScreenInfoProvider : IScreenInfoProvider
{
    private bool _queried;
    private int _desktopWidth;
    private int _desktopHeight;
    private IReadOnlyList<(int Width, int Height)> _modes = [];

    public int Priority => 0;

    public Task<bool> IsApplicableAsync() =>
        Task.FromResult(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));

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
            uint displayId = CGMainDisplayID();

            // Desktop resolution from the current display mode
            IntPtr currentMode = CGDisplayCopyDisplayMode(displayId);
            if (currentMode != IntPtr.Zero)
            {
                _desktopWidth = CGDisplayModeGetWidth(currentMode);
                _desktopHeight = CGDisplayModeGetHeight(currentMode);
                CFRelease(currentMode);
            }

            // All supported display modes
            IntPtr modesArray = CGDisplayCopyAllDisplayModes(displayId, IntPtr.Zero);
            if (modesArray != IntPtr.Zero)
            {
                try
                {
                    int count = CFArrayGetCount(modesArray);
                    var modes = new HashSet<(int, int)>();

                    for (int i = 0; i < count; i++)
                    {
                        IntPtr modePtr = CFArrayGetValueAtIndex(modesArray, i);
                        if (modePtr == IntPtr.Zero) continue;

                        int w = CGDisplayModeGetWidth(modePtr);
                        int h = CGDisplayModeGetHeight(modePtr);
                        if (w > 0 && h > 0)
                            modes.Add((w, h));
                    }

                    if (modes.Count > 0)
                        _modes = modes.OrderBy(m => m.Item1 * m.Item2).ThenBy(m => m.Item1).ToList();
                }
                finally
                {
                    CFRelease(modesArray);
                }
            }
        }
        catch
        {
            // Keep defaults
        }
    }

    // ----------------------------------------------------------------
    // P/Invoke — CoreGraphics
    // ----------------------------------------------------------------

    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [DllImport(CoreGraphics)]
    private static extern uint CGMainDisplayID();

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGDisplayCopyDisplayMode(uint display);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CGDisplayCopyAllDisplayModes(uint display, IntPtr options);

    [DllImport(CoreGraphics)]
    private static extern int CGDisplayModeGetWidth(IntPtr mode);

    [DllImport(CoreGraphics)]
    private static extern int CGDisplayModeGetHeight(IntPtr mode);

    [DllImport(CoreGraphics)]
    private static extern void CFRelease(IntPtr obj);

    [DllImport(CoreGraphics)]
    private static extern int CFArrayGetCount(IntPtr array);

    [DllImport(CoreGraphics)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, int index);
}
