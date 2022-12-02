using Microsoft.Extensions.Logging.Abstractions;

namespace Log4n.File;

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerProcessor _queueProcessor;

    public FileLogger(string categoryName, FileLoggerProcessor queueProcessor)
    {
        _categoryName = categoryName;
        _queueProcessor = queueProcessor;
    }

    public SimpleFileFormatter Formatter { get; set; } = null!;
    public IExternalScopeProvider ScopeProvider { get; set; } = null!;

    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    [ThreadStatic] private static StringWriter? s_tStringWriter;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        s_tStringWriter ??= new StringWriter();
        LogEntry<TState> logEntry = new(logLevel, _categoryName, eventId, state, exception, formatter);
        DateTime now = DateTime.Now;
        Formatter.Write(in logEntry, now, ScopeProvider, s_tStringWriter);

        var sb = s_tStringWriter.GetStringBuilder();
        if (sb.Length == 0)
        {
            return;
        }

        string message = sb.ToString();
        sb.Clear();
        if (sb.Capacity > 1024)
        {
            sb.Capacity = 1024;
        }

        _queueProcessor.EnqueueMessage(new LogMessageEntry(now, message, logEntry.Category));
    }
}
