using System.Text;

namespace Log4n.File;

public class FileAppender : IDisposable
{
    private readonly FileAppenderOptions _options;

    private FileStream _stream;

    // 日志文件绝对路径
    private readonly string _file;

    public FileAppender(FileAppenderOptions options)
    {
        _options = options;
        _file = Path.IsPathRooted(_options.File)
            ? _options.File
            : Path.Combine(AppContext.BaseDirectory, _options.File);
        DirectoryInfo? parent = Directory.GetParent(_file);
        if (parent == null)
        {
            throw new Exception("no parent folder:" + _file);
        }

        if (!parent.Exists)
        {
            parent.Create();
        }

        _stream = OpenFile();
    }

    private DateTime _lastWriteTime;
    private int _count;

    public void Write(DateTime createTime, string message)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            if (_stream.Length + buffer.Length > _options.MaxFileSize)
            {
                _stream.Dispose();
                Archive(_file, _options);
                _stream = OpenFile();
            }

            _stream.Write(buffer);
            if (++_count % 100 == 0 || (createTime - _lastWriteTime).TotalMilliseconds > 300)
            {
                _stream.Flush();
            }

            _lastWriteTime = createTime;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void Archive(string file, FileAppenderOptions options)
    {
        string fileName = Path.GetFileName(file);
        string extension = Path.GetExtension(file);
        string pattern = fileName[..fileName.IndexOf('.')] + ".**.**" + extension;
        DirectoryInfo parent = Directory.GetParent(file)!;
        FileInfo[] fileInfos = parent.GetFiles(pattern);
        int logArchiveNo = 0;
        if (fileInfos.Length > 0)
        {
            Array.Sort(fileInfos, (a, b) =>
            {
                if (a.Name.Length == b.Name.Length)
                {
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                }

                return a.Name.Length - b.Name.Length;
            });

            for (int i = 0; i < fileInfos.Length - options.MaxHistory; i++)
            {
                fileInfos[i].Delete();
            }

            FileInfo fileInfo = fileInfos.Last();
            logArchiveNo = GetLogArchiveNo(fileInfo.Name) + 1;
        }

        string archiveFileName = Path.GetFileNameWithoutExtension(file) + "." + DateTime.Now.ToString("yyyyMMdd") +
                                 "." + logArchiveNo + extension;
        System.IO.File.Move(file, Path.Combine(parent.FullName, archiveFileName), true);
    }

    private static int GetLogArchiveNo(string fileName)
    {
        int lastDotIndex = fileName.LastIndexOf('.');
        int secondToLastDotIndex = fileName.LastIndexOf('.', lastDotIndex - 1);
        string s = fileName[(secondToLastDotIndex + 1)..lastDotIndex];
        return int.TryParse(s, out int no) ? no : 0;
    }

    private FileStream OpenFile()
    {
        return System.IO.File.Open(_file, FileMode.Append, FileAccess.Write, FileShare.Read);
    }

    public void Dispose() => _stream.Dispose();
}

public class FileAppenderOptions
{
    /// <summary>
    /// 日志名
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 文件名, 如为相对路径, 则相对当前项目目录
    /// </summary>
    public string File { get; set; } = "";

    /// <summary>
    /// If log archive cleanup should occur when the application starts.
    /// </summary>
    public bool CleanHistoryOnStart { get; init; }

    /// <summary>
    /// The maximum size of log file before it is archived.
    /// </summary>
    public int MaxFileSize { get; init; }

    /// <summary>
    /// The maximum amount of size log archives can take before being deleted.
    /// </summary>
    public int TotalSizeCap { get; init; }

    /// <summary>
    /// The maximum number of archive log files to keep (defaults to 7).
    /// </summary>
    public int MaxHistory { get; init; }
}
