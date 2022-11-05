using Discord;
using Discord.Interactions;

namespace Sentinel;

[Discord.Interactions.RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("cfg","Commands to tweak the server configuration")]
public class ConfigCommands : InteractionModuleBase
{
    private Sentinel _core;
    public ConfigCommands(Sentinel core)
    {
        _core = core;
    }

    [Discord.Interactions.RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    [SlashCommand("flagchannel","Set the channel AutoMod flags are dumped in")]
    public async Task FlagChannel(IMessageChannel channel)
    {
        var data = _core.GetDbContext();
        var srv = await data.GetServerConfig(Context.Guild.Id);
        srv.FlagChannel = channel.Id;
        await data.SaveChangesAsync();
        await RespondAsync("Done ✅",ephemeral: true);
    }
    
    [Discord.Interactions.RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    [SlashCommand("modrole","Set the role for Moderators")]
    public async Task FlagChannel(IRole role)
    {
        var data = _core.GetDbContext();
        var srv = await data.GetServerConfig(Context.Guild.Id);
        srv.ModRole = role.Id;
        await data.SaveChangesAsync();
        await RespondAsync("Done ✅",ephemeral: true);
    }
    
    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "addfactcheck", description: "Add a factcheck response")]
    public async Task AddQuote(string text, Config.FactCheck.CheckType type)
    {
        Config cfg = _core.GetConfig();
        cfg.FactChecks.Add(new Config.FactCheck(text,type));
        await _core.UpdateConfig();
        await RespondAsync($"Added {text} as a {type}");
    }
    
}