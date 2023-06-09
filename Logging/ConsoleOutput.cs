namespace Sentinel.Logging;

public class ConsoleOutput : ILogOutput
{
    public LogType LogLevel { get; set; } = LogType.Info | LogType.Debug | LogType.Critical;
    
    public async Task Log(LogEntry entry)
    {
        if((LogLevel & entry.Level) == entry.Level)
        {
            Console.WriteLine($"{entry.Timestamp:dd/MM/yy HH:mm:ss} [{entry.Source}] {entry.Message}");
        }
    }

    public void SetLogLevel(LogType level)
    {
        LogLevel = SentinelLogging.GetLogLevelFlag(level);
    }
}