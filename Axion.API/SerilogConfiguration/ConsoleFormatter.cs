using Serilog.Events;
using Serilog.Formatting;

namespace Axion.API.SerilogConfiguration;

public class ConsoleFormatter : ITextFormatter
{
    private const string Reset = "\x1b[0m";
    private const string Blue = "\x1b[34m";
    private const string Yellow = "\x1b[33m";
    private const string Red = "\x1b[31m";
    private const string WhiteOnRed = "\x1b[97;41m";

    public void Format(LogEvent logEvent, TextWriter output)
    {
        // Timestamp
        output.Write($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss}]");
        output.Write(" ");

        // Level with color
        var levelColor = logEvent.Level switch
        {
            LogEventLevel.Information => Blue,
            LogEventLevel.Warning => Yellow,
            LogEventLevel.Error => Red,
            LogEventLevel.Fatal => WhiteOnRed,
            _ => Reset
        };

        var levelText = logEvent.Level switch
        {
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Verbose => "VRB",
            _ => "???"
        };

        output.Write(levelColor);
        output.Write($"[{levelText}]");
        output.Write(Reset);
        output.Write(" ");

        // ProcessId
        if (logEvent.Properties.TryGetValue("ProcessId", out var processId))
        {
            output.Write($"[{processId.ToString().Trim('\"')}]");
        }
        output.Write(" ");

        // Message
        // TODO: write log message manually
        output.Write(logEvent.RenderMessage());
        output.WriteLine();

        // Exception if present
        if (logEvent.Exception != null)
        {
            output.WriteLine(logEvent.Exception.ToString());
        }
    }
}