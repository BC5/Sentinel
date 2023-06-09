namespace Sentinel.Logging;

public interface ILogOutput
{
    public Task Log(LogEntry entry);

    public void SetLogLevel(LogType level);
}