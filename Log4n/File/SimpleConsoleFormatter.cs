using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Log4n.File;

public class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;

    public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
        : base("simple1")
    {
        ReloadLoggerOptions(options.CurrentValue);
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
    }

    private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
    {
        _formatterOptions = options;
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }

    private SimpleConsoleFormatterOptions _formatterOptions = null!;

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        string timestamp = GetCurrentDateTime().ToString(_formatterOptions.TimestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff");
        textWriter.Write(timestamp);
        textWriter.Write(" ");

        LogLevel logLevel = logEntry.LogLevel;
        ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
        string logLevelString = GetLogLevelString(logLevel);
        WriteColoredMessage(textWriter, logLevelString, logLevelColors.Background, logLevelColors.Foreground);
        textWriter.Write(" ");

        textWriter.Write(logEntry.Category);
        textWriter.Write(": ");

        if (_formatterOptions.IncludeScopes)
        {
            WriteScopeInformation(textWriter, scopeProvider, _formatterOptions.SingleLine);
        }

        textWriter.Write(message);

        if (logEntry.Exception != null)
        {
            textWriter.WriteLine();
            textWriter.Write(logEntry.Exception.ToString());
        }

        textWriter.WriteLine();
    }

    private DateTimeOffset GetCurrentDateTime()
    {
        return _formatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
    }

    internal static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO ",
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRITI",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
    {
        bool disableColors = (_formatterOptions.ColorBehavior == LoggerColorBehavior.Disabled) ||
                             (_formatterOptions.ColorBehavior == LoggerColorBehavior.Default &&
                              Console.IsOutputRedirected);
        if (disableColors)
        {
            return new ConsoleColors(null, null);
        }

        // We must explicitly set the background color if we are setting the foreground color,
        // since just setting one can look bad on the users console.
        return logLevel switch
        {
            LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColors(null, null)
        };
    }

    internal static void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider,
        bool singleLine)
    {
        if (scopeProvider != null)
        {
            bool paddingNeeded = !singleLine;
            scopeProvider.ForEachScope((scope, state) =>
            {
                state.Write(" => ");
                state.Write(scope);
            }, textWriter);

            if (!paddingNeeded && !singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }
        }
    }

    private readonly struct ConsoleColors
    {
        public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
        {
            Foreground = foreground;
            Background = background;
        }

        public ConsoleColor? Foreground { get; }

        public ConsoleColor? Background { get; }
    }

    private static void WriteColoredMessage(TextWriter textWriter, string message, ConsoleColor? background,
        ConsoleColor? foreground)
    {
        // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));
        }

        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));
        }

        textWriter.Write(message);
        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultForegroundColor); // reset to default foreground color
        }

        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultBackgroundColor); // reset to the background color
        }
    }
}
