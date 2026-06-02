using Serilog.Core;
using Serilog.Events;

namespace ClientCore;

public static class SerilogHelper
{
    /// <summary>
    /// Controls the minimum logging level at runtime.
    /// </summary>
    public static readonly LoggingLevelSwitch LoggingLevelSwitch = new()
    {
        MinimumLevel = LogEventLevel.Information
    };

    /// <summary>
    /// Output template matching the original Logger format: dd.MM. HH:mm:ss.fff    message
    /// </summary>
    public const string OutputTemplate = "{Timestamp:dd.MM. HH:mm:ss.fff}    {Message:lj}{NewLine}{Exception}";
}
