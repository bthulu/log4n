using Microsoft.Extensions.Logging.Abstractions;

namespace Log4n.File;

public class SimpleFileFormatter
{
    public SimpleFileFormatter()
    {
    }

    public void Write<TState>(in LogEntry<TState> logEntry, DateTime now, IExternalScopeProvider scopeProvider,
        TextWriter textWriter)
    {
        string? message = logEntry.State?.ToString();
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        textWriter.Write(now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        textWriter.Write(" ");

        string logLevelString = SimpleConsoleFormatter.GetLogLevelString(logEntry.LogLevel);
        textWriter.Write(logLevelString);
        textWriter.Write(" ");

        textWriter.Write(logEntry.Category);
        textWriter.Write(": ");

        SimpleConsoleFormatter.WriteScopeInformation(textWriter, scopeProvider, true);

        textWriter.Write(message);

        if (logEntry.Exception != null)
        {
            textWriter.WriteLine();
            textWriter.Write(logEntry.Exception.ToString());
        }

        textWriter.WriteLine();
    }
}
