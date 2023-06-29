using Discord;
using Discord.Interactions;

namespace Sentinel;


[DefaultMemberPermissions(GuildPermission.Administrator)]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("roleticker","Commands to adjust roleticker")]
public class RoleTicker : InteractionModuleBase
{
    private Sentinel _core;
    private Config _config;

    public RoleTicker(Sentinel core)
    {
        _core = core;
        _config = core.GetConfig();
    }
    
    [SlashCommand("new", "Add a role ticker")]
    public async Task NewRoleTicker(IRole role)
    {
        await DeferAsync(ephemeral: true);
        var users = await Context.Guild.GetUsersAsync();
        int roleusers = users.Where(u => u.RoleIds.Contains(role.Id)).Count();
        var msg = await Context.Channel.SendMessageAsync($"{role.Mention} - {roleusers:n0}");
        Config.RoleTicker rt = new Config.RoleTicker()
        {
            RoleId = role.Id,
            MessageId = msg.Id,
            ChannelId = Context.Channel.Id,
            GuildId = Context.Guild.Id
        };
        _config.RoleTickers.Add(rt);
        await _core.UpdateConfig();
        await FollowupAsync("Done");
    }
}