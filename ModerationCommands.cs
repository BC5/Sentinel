using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Sentinel;

public class ModerationCommands : InteractionModuleBase
{

    private AutoMod _autoMod;
    private Sentinel _sentinel;
    private Detention _detention;
    
    public ModerationCommands(AutoMod am, Sentinel sentinel, Detention detention)
    {
        _autoMod = am;
        _sentinel = sentinel;
        _detention = detention;
    }

    /*
    [ComponentInteraction("sentinel-unmute-*-*")]
    public async Task AntiSierraAction(string srv, string usr)
    {
        var guild = await Context.Client.GetGuildAsync(ulong.Parse(srv));
        if (guild == null)
        {
            await RespondAsync("Can't find the server, I might've been kicked");
            return;
        }
        var user = await guild.GetUserAsync(ulong.Parse(usr));
        if (user == null)
        {
            await RespondAsync("Can't you in the server, You might've been kicked");
            return;
        }

        await user.RemoveTimeOutAsync();
        try
        {
            var msg = (SocketUserMessage) ((IComponentInteraction) Context.Interaction).Message;
            await msg.DeleteAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        await RespondAsync("Unmuted in TCv3");
        await _sentinel.DMBC5($"I just unmuted {user.Mention}");
        
    }
    */

    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("idiot","Brand a user with the idiot role")]
    public async Task Idiot(IGuildUser user, float duration, bool hours = false)
    {
        await DeferAsync();
        var data = _sentinel.GetDbContext();
        try
        {
            ServerUser su = await data.GetServerUser(user);
            ServerConfig scfg = await data.GetServerConfig(user.GuildId);

            if (!scfg.IdiotRole.HasValue)
            {
                await FollowupAsync("No idiot role set");
                return;
            }

            TimeSpan durationts = hours ? TimeSpan.FromHours(duration) : TimeSpan.FromDays(duration);
            
            await _detention.ModifySentence(user, durationts, data);
            await data.SaveChangesAsync();
            if (durationts > TimeSpan.Zero)
            {
                if (durationts > TimeSpan.FromHours(23))
                {
                    await FollowupAsync($"{user.Mention} <@&{scfg.IdiotRole.Value}> ({durationts:%d} days)");
                }
                else
                {
                    await FollowupAsync(
                        $"{user.Mention} <@&{scfg.IdiotRole.Value}> ({durationts:h' hours 'm' minutes'})");
                }
            }
            else
            {
                durationts = TimeSpan.Zero - durationts;
                if (durationts > TimeSpan.FromHours(23))
                {
                    await FollowupAsync($"{user.Mention} sentence shortened by {durationts:%d} days");
                }
                else
                {
                    await FollowupAsync($"{user.Mention} sentence shortened by {durationts:h' hours 'm' minutes'}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("unidiot","Release a user from idiotdom")]
    public async Task UnIdiot(IGuildUser user)
    {
        try
        {
            var data = _sentinel.GetDbContext();
            ServerUser su = await data.GetServerUser(user);
            ServerConfig scfg = await data.GetServerConfig(user.GuildId);

            if (!scfg.IdiotRole.HasValue)
            {
                await RespondAsync("No idiot role set", ephemeral: true);
                return;
            }

            if (su.IdiotedUntil == null)
            {
                await RespondAsync("I didn't idiot them lol",ephemeral:true);
                return;
            }
            
            await _detention.Unidiot(user, su, scfg.IdiotRole.Value);
            await data.SaveChangesAsync();
            await RespondAsync($"{user.Mention} no longer an <@&{scfg.IdiotRole.Value}>!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    [SlashCommand("idiotdays","How many days do they have left in detention?")]
    public async Task DaysRemaining(IGuildUser user)
    {
        var data = _sentinel.GetDbContext();
        ServerUser su = await data.GetServerUser(user);
        if (su.IdiotedUntil != null && su.IdiotedUntil > DateTime.Now)
        {
            DateTimeOffset tsdto = su.IdiotedUntil.Value;
            long ts = tsdto.ToUnixTimeSeconds();
            await RespondAsync($"{user.Mention}'s sentence ends <t:{ts}:R>");
        }
        else
        {
            await RespondAsync("I don't think they're idioted by me", ephemeral: true);
        }
    }


    [MessageCommand("Report")]
    public async Task Flag(IMessage msg)
    {
        bool x = await _autoMod.Flag(msg,Context.Guild.Id,AutoMod.FlagReason.REPORT,$"Flagged by {Context.User.Username}");
        if(x) await RespondAsync("Report Submitted to Moderators", ephemeral: true);
        await RespondAsync("Failed. Admin probably hasn't set this up.", ephemeral: true);
    }
    
}