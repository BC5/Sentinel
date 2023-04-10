using Discord.WebSocket;

namespace Sentinel.Bot.Procedures;

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
    public SentinelBot _bot;
    public Config _config;

    public ProcedureContext(SentinelBot bot)
    {
        _discord = bot.GetClient();
        _config = bot.GetConfig();
    }
}