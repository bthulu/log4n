using System.Collections.Concurrent;
using System.Runtime.Versioning;

namespace Log4n.File;

[UnsupportedOSPlatform("browser")]
public class FileLoggerProcessor : IDisposable
{
    private const int MaxQueuedMessages = 1024;

    private readonly BlockingCollection<LogMessageEntry> _messageQueue = new(MaxQueuedMessages);

    private readonly Thread _outputThread;

    // key=FileLoggerOptions.Name
    private Dictionary<string, FileAppender> _appenders = new();
    private FileLoggerOptions _loggerOptions = null!;

    public FileLoggerProcessor(FileLoggerOptions options)
    {
        ReloadLoggerOptions(options);
        // Start Console message queue processor
        _outputThread = new Thread(ProcessLogQueue)
        {
            IsBackground = true, Name = "File logger queue processing thread"
        };
        _outputThread.Start();
    }

    public void ReloadLoggerOptions(FileLoggerOptions options)
    {
        _loggerOptions = options;
        _appenders = options.Appenders.ToDictionary(o => o.Name, o => new FileAppender(o));
    }

    public void EnqueueMessage(LogMessageEntry message)
    {
        if (!_messageQueue.IsAddingCompleted)
        {
            try
            {
                _messageQueue.Add(message);
                return;
            }
            catch (InvalidOperationException) { }
        }

        // Adding is completed so just log the message
        try
        {
            WriteMessage(message);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private void WriteMessage(LogMessageEntry entry)
    {
        string logName = _loggerOptions.Route(entry.Category);
        if (!_appenders.TryGetValue(logName, out var writer))
        {
            return;
        }

        writer.Write(entry.CreateTime, entry.Message);
    }

    private void ProcessLogQueue()
    {
        foreach (LogMessageEntry entry in _messageQueue.GetConsumingEnumerable())
        {
            WriteMessage(entry);
        }
    }

    public void Dispose()
    {
        _messageQueue.CompleteAdding();

        try
        {
            _outputThread.Join(1500); // with timeout to wait all messages are written to file
        }
        catch (ThreadStateException) { }

        ReleaseAppenders();
    }

    private void ReleaseAppenders()
    {
        foreach (FileAppender writer in _appenders.Values)
        {
            try
            {
                writer.Dispose();
            }
            catch
            {
                // ignored
            }
        }
    }
}
