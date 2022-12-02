namespace Log4n.File;

public interface IFileLoggerRouter
{
    public delegate string Route(string category);
}
