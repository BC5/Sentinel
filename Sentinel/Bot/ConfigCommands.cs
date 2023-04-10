using Discord;
using Discord.Interactions;

namespace Sentinel.Bot;

[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("cfg", "Commands to tweak the server configuration")]
public class ConfigCommands : InteractionModuleBase
{
    private SentinelBot _bot;
    public ConfigCommands(SentinelBot bot)
    {
        _bot = bot;
    }

    [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    [SlashCommand("flagchannel", "Set the channel AutoMod flags are dumped in")]
    public async Task FlagChannel(IMessageChannel channel)
    {
        var data = _bot.GetDbContext();
        var srv = await data.GetServerConfig(Context.Guild.Id);
        srv.FlagChannel = channel.Id;
        await data.SaveChangesAsync();
        await RespondAsync("Done ✅", ephemeral: true);
    }

    [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    [SlashCommand("modrole", "Set the role for Moderators")]
    public async Task FlagChannel(IRole role)
    {
        var data = _bot.GetDbContext();
        var srv = await data.GetServerConfig(Context.Guild.Id);
        srv.ModRole = role.Id;
        await data.SaveChangesAsync();
        await RespondAsync("Done ✅", ephemeral: true);
    }

    [RequireOwner]
    [SlashCommand(name: "addfactcheck", description: "Add a factcheck response")]
    public async Task AddQuote(string text, Config.FactCheck.CheckType type)
    {
        Config cfg = _bot.GetConfig();
        cfg.FactChecks.Add(new Config.FactCheck(text, type));
        await _bot.UpdateConfig();
        await RespondAsync($"Added {text} as a {type}");
    }

}