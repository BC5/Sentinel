
using Discord;

namespace Sentinel.Logging;

public class SentinelLogging
{

    public List<LogEntry>? LogEntries { get; set; } = new();
    public List<ILogOutput> LogOutputs { get; set; } = new();
    
    public SentinelLogging()
    {
        
    }

    public async Task LogAsync(LogEntry log)
    {
        if (LogEntries != null) LogEntries.Add(log);
        List<Task> tasks = new();
        foreach (var o in LogOutputs)
        {
            tasks.Add(o.LogAsync(log));
        }
        await Task.WhenAll(tasks);
    }

    public Task LogAsync(LogType level, string source, string message)
    {
        return LogAsync(new LogEntry(source,message));
    }

    public void Log(LogEntry log)
    {
        if (LogEntries != null) LogEntries.Add(log);
        foreach (var o in LogOutputs)
        {
            o.Log(log);
        }
    }
    
    public void Log(LogType level, string source, string message)
    {
        Log(new LogEntry(source,message));
    }

    public Task Info(string source, string message)
    {
        return LogAsync(LogType.Info, source, message);
    }
    
    public Task Fine(string source, string message)
    {
        return LogAsync(LogType.Fine, source, message);
    }
    
    public Task Error(string source, string message)
    {
        return LogAsync(LogType.Error, source, message);
    }

    public static LogType FromSeverity(LogSeverity severity)
    {
        switch (severity)
        {
            case LogSeverity.Debug: return LogType.Debug;
            case LogSeverity.Verbose: return LogType.Fine;
            case LogSeverity.Info: return LogType.Info;
            case LogSeverity.Warning: return LogType.Error;
            case LogSeverity.Error: return LogType.Error;
            case LogSeverity.Critical: return LogType.Critical;
            default: return LogType.Info;
        }
    }
    
    public static LogType GetLogLevelFlag(LogType level)
    {
        LogType LogLevel = level;
        
        switch (level)
        {
            case LogType.Disabled:
                LogLevel = LogType.Disabled;
                break;
            case LogType.Critical:
                LogLevel = LogType.Critical;
                break;
            case LogType.Error:
                LogLevel = LogType.Critical | LogType.Error;
                break;
            case LogType.Info:
                LogLevel = LogType.Critical | LogType.Error | LogType.Info;
                break;
            case LogType.Fine:
                LogLevel = LogType.Critical | LogType.Error | LogType.Info | LogType.Fine;
                break;
            case LogType.Debug:
                LogLevel = LogType.Critical | LogType.Error | LogType.Info | LogType.Fine | LogType.Debug;
                break;
        }

        return LogLevel;
    }

}