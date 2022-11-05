using Discord.WebSocket;

namespace Sentinel.Procedures;

public interface ISentinelAction
{
    public Task<ActionStatus> Execute(ProcedureContext context);
}

public enum ActionStatus
{
    SUCCESS,
    FAILURE,
    CRITICAL_FAILURE
}

public class ProcedureContext
{
    public DiscordSocketClient _discord;
    public Sentinel _bot;
    public Config _config;

    public ProcedureContext(Sentinel bot)
    {
        _discord = bot.GetClient();
        _config = bot.GetConfig();
    }
}