namespace Sentinel.Logging;

public interface ILogOutput
{
    public Task LogAsync(LogEntry entry);
    public void Log(LogEntry log);

    public void SetLogLevel(LogType level);
}