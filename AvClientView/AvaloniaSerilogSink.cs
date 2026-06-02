using Avalonia.Logging;

namespace AvClientView;

public class AvaloniaSerilogSink : ILogSink
{
    public bool IsEnabled(LogEventLevel level, string area)
    {
        // Translate Avalonia log levels to Serilog levels to check eligibility
        var serilogLevel = ConvertLevel(level);
        return Serilog.Log.IsEnabled(serilogLevel);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        var serilogLevel = ConvertLevel(level);
        Serilog.Log.ForContext("Area", area)
           .ForContext("SourceContext", source?.GetType().FullName ?? "Avalonia")
           .Write(serilogLevel, messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        var serilogLevel = ConvertLevel(level);
        Serilog.Log.ForContext("Area", area)
           .ForContext("SourceContext", source?.GetType().FullName ?? "Avalonia")
           .Write(serilogLevel, messageTemplate, propertyValues);
    }

    private Serilog.Events.LogEventLevel ConvertLevel(LogEventLevel level) =>
        level switch
        {
            LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            _ => Serilog.Events.LogEventLevel.Information,
        };
}
