namespace Log4n.File;

public sealed class FileLoggerOptions
{
    public delegate string Router(string category);

    public Router Route { get; set; } = null!;

    public FileAppenderOptions[] Appenders { get; set; } = null!;
}
