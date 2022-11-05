using Discord;
using Discord.WebSocket;

namespace Sentinel;

public class AutoMod
{

    private static int cacheSize = 50;
    public Queue<ulong> RecentlyFlaggedMessages;
    private Sentinel _core;
    private DiscordSocketClient _discord;
    
    public AutoMod(Sentinel core, DiscordSocketClient discord)
    {
        _core = core;
        _discord = discord;
        RecentlyFlaggedMessages = new();
    }

    public async Task<bool> Flag(IMessage msg, ulong server, FlagReason type, string message)
    {
        var data = _core.GetDbContext();
        var srv = await data.GetServerConfig(server);
        if (!srv.FlagChannel.HasValue) return false;
        var channel = (ISocketMessageChannel) _discord.GetChannel(srv.FlagChannel.Value);
        var eb = new EmbedBuilder();
        eb.WithTitle("Message Flagged");
        eb.WithAuthor(msg.Author);
        eb.WithDescription(msg.Content + $"\n\n[Jump]({msg.GetJumpUrl()})");
        eb.WithFooter($"{message}");
        switch (type)
        {
            case FlagReason.REPORT:
                eb.WithColor(0xFF0000);
                break;
            case FlagReason.AUTOFLAG:
                eb.WithColor(0xFEFE6C);
                break;
        }
        
        if (type is FlagReason.REPORT && srv.ModRole != null)
        {
            await channel.SendMessageAsync($"<@&{srv.ModRole}>",embed:eb.Build());
        }
        else
        {
            await channel.SendMessageAsync(embed:eb.Build());
        }

        return true;
    }

    public enum FlagReason
    {
        REPORT,
        AUTOFLAG
    }
    
}