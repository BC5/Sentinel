namespace Sentinel;

public interface ISentinelPlugin
{
    public ISentinelModule[] Modules { get; }
    public string ModuleName { get; }
}