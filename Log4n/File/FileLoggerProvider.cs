using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;

namespace Log4n.File;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("File")]
public class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerProcessor _messageQueue;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private readonly SimpleFileFormatter _formatter = new();

    private readonly IDisposable _optionsReloadToken;

    public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
    {
        _messageQueue = new FileLoggerProcessor(options.CurrentValue);
        _optionsReloadToken = options.OnChange(_messageQueue.ReloadLoggerOptions);
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName,
        name => new FileLogger(name, _messageQueue) { ScopeProvider = _scopeProvider, Formatter = _formatter });


    public void Dispose()
    {
        _loggers.Clear();
        _optionsReloadToken.Dispose();
        _messageQueue.Dispose();
    }
}
