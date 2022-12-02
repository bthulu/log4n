namespace Log4n.File;

public readonly struct LogMessageEntry
{
    public LogMessageEntry(DateTime createTime, string message, string category)
    {
        CreateTime = createTime;
        Message = message;
        Category = category;
    }

    public readonly DateTime CreateTime;
    public readonly string Message;
    public readonly string Category;
}
