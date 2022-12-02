using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Log4n.File;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerOptions> configure)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FileLoggerOptions>, LoggerFormatterConfigureOptions<FileLoggerOptions>>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<FileLoggerOptions>, LoggerFormatterOptionsChangeTokenSource<FileLoggerOptions>>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<FileLoggerOptions, FileLoggerProvider>(builder.Services);

        builder.Services.Configure(configure);

        return builder;
    }

    [UnsupportedOSPlatform("browser")]
    internal sealed class LoggerFormatterConfigureOptions<TOptions> : ConfigureFromConfigurationOptions<TOptions>
        where TOptions : class
    {
        public LoggerFormatterConfigureOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration) :
            base(providerConfiguration.Configuration.GetSection("FileOptions"))
        {
        }
    }

    [UnsupportedOSPlatform("browser")]
    internal sealed class LoggerFormatterOptionsChangeTokenSource<TOptions> : ConfigurationChangeTokenSource<TOptions>
        where TOptions : class
    {
        public LoggerFormatterOptionsChangeTokenSource(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration.GetSection("FileOptions"))
        {
        }
    }

}
