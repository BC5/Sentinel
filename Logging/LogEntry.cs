namespace Sentinel.Logging;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogType Level { get; set; }
        
    public string Source { get; set; }
    public string Message { get; set; }

    public LogEntry(string source, string message, LogType level = LogType.Info)
    {
        Source = source;
        Message = message;
        Level = level;
    }
}