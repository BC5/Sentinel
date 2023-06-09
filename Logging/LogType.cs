namespace Sentinel.Logging;

[Flags]
public enum LogType
{
    Disabled = 0b_0000_0000,
    Critical = 0b_0000_0001,
    Error    = 0b_0000_0010,
    Info     = 0b_0000_0100,
    Fine     = 0b_0000_1000,
    Debug    = 0b_0001_0000

}