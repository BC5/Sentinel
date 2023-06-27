using Autofac.Core;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;

namespace Sentinel;

public class ModerationCommands : InteractionModuleBase
{

    private AutoMod _autoMod;
    private Sentinel _sentinel;
    private Detention _detention;
    private Data _data;
    
    public ModerationCommands(AutoMod am, Sentinel sentinel, Detention detention, Data data)
    {
        _autoMod = am;
        _sentinel = sentinel;
        _detention = detention;
        _data = data;
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
    [SlashCommand("multimute","Mute up to 6 users in one go")]
    public async Task MultiMute(string reason, int mins, IGuildUser t1, IGuildUser? t2 = null, IGuildUser? t3 = null, IGuildUser? t4 = null, IGuildUser? t5 = null, IGuildUser? t6 = null)
    {
        await DeferAsync();
        List<IGuildUser> mutes = new();
        mutes.Add(t1);
        if(t2 != null) mutes.Add(t2);
        if(t3 != null) mutes.Add(t3);
        if(t4 != null) mutes.Add(t4);
        if(t5 != null) mutes.Add(t5);
        if(t6 != null) mutes.Add(t6);

        string pings = "";

        int i = 0;
        foreach (var m in mutes)
        {
            if(m.GuildPermissions.ModerateMembers) continue;
            i++;
            pings = pings + m.Mention + " ";
            await _data.AddModlog(Context.User, m, ModLog.ModAction.Mute, $"{mins:n0}m {reason}");
            await m.SetTimeOutAsync(TimeSpan.FromMinutes(mins), new RequestOptions() {AuditLogReason = $"Multimute by {Context.User.Username}"});
        }

        var eb = new EmbedBuilder();
        eb.WithTitle($"Muted {i} users for {mins} minutes");
        eb.WithDescription($"**Reason:** {reason}");
        eb.WithColor(255,0,0);
        await FollowupAsync(pings, embed: eb.Build());
    }
    

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("autopurge","Setup autopurge for a channel")]
    public async Task SetupPurge(ITextChannel channel)
    {
        ServerConfig scfg = await _data.GetServerConfig(Context.Guild.Id);

        PurgeConfiguration? existing = scfg.PurgeConfig.FirstOrDefault(x => x.ChannelID == channel.Id);

        if (existing != null)
        {
            scfg.PurgeConfig.Remove(existing);
            await RespondAsync($"I won't autopurge {channel.Mention} anymore");
        }
        else
        {
            var pc = new PurgeConfiguration()
            {
                ChannelID = channel.Id,
                LastPurge = DateTimeOffset.UnixEpoch
            };
            scfg.PurgeConfig.Add(pc);
            await RespondAsync($"{channel.Mention} now set to autopurge");
        }
        await _data.SaveChangesAsync();
    }

    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("idiot","Brand a user with the idiot role")]
    public async Task Idiot(IGuildUser user, float duration, bool hours = false, string? reason = null)
    {
        await DeferAsync();
        try
        {
            ServerUser su = await _data.GetServerUser(user);
            ServerConfig scfg = await _data.GetServerConfig(user.GuildId);

            if (!scfg.IdiotRole.HasValue)
            {
                await FollowupAsync("No idiot role set");
                return;
            }

            TimeSpan durationts = hours ? TimeSpan.FromHours(duration) : TimeSpan.FromDays(duration);
            
            await _detention.ModifySentence(user, durationts, _data);
            await _data.SaveChangesAsync();
            if (durationts > TimeSpan.Zero)
            {
                await _data.AddModlog(Context.User, user, ModLog.ModAction.Detain, $"{durationts:%d}d {reason}");
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
    [SlashCommand("juvelock","Better post juve.")]
    public async Task JuveCheck(IGuildUser user)
    {
        ServerUser su = await _data.GetServerUser(user);
        su.Juvecheck = !su.Juvecheck;
        if (su.Juvecheck)
        {
            await RespondAsync($"{user.Mention} post juve.");
        }
        else
        {
            await RespondAsync($"{user.Mention} juve is optional. for now...");
        }
        await _data.SaveChangesAsync();
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("unidiot","Release a user from idiotdom")]
    public async Task UnIdiot(IGuildUser user, string? reason = null)
    {
        try
        {
            ServerUser su = await _data.GetServerUser(user);
            ServerConfig scfg = await _data.GetServerConfig(user.GuildId);

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

            await _data.AddModlog(Context.User, user, ModLog.ModAction.Release, reason);
            await _detention.Unidiot(user, su, scfg.IdiotRole.Value);
            await _data.SaveChangesAsync();
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
        ServerUser su = await _data.GetServerUser(user);
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

    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("purgeuser", "Remove a bunch of a user's messages")]
    public async Task PurgeUser(IGuildUser user, TimeSpan duration)
    {
        try
        {
            await DeferAsync();

            DateTime cutoff = DateTime.Now - duration;
        
            var msgCollections = await Context.Channel.GetMessagesAsync(250).ToListAsync();

            List<ulong> purgeList = new();
        
            IMessage? earliest = null;
            int loops = 0;
            
            while (loops == 0 || (loops < 20 && earliest.Timestamp > cutoff))
            {
                loops++;
                foreach (var msl in msgCollections)
                {
                    foreach (IMessage msg in msl)
                    {
                        if (earliest == null || msg.Timestamp < earliest.Timestamp) earliest = msg;

                        if (msg.Timestamp < cutoff) continue;
                
                        if (msg.Author.Id == user.Id && !purgeList.Contains(msg.Id)) purgeList.Add(msg.Id);
                    }
                }

                if (earliest.Timestamp > cutoff)
                {
                    msgCollections = await Context.Channel.GetMessagesAsync(earliest, Direction.Before, 250).ToListAsync();
                }
            }
            
            Console.WriteLine("Earliest: " + earliest.GetJumpUrl());

            ITextChannel channel = (ITextChannel) Context.Channel;
            await channel.DeleteMessagesAsync(purgeList);

            if (loops == 20)
            {
                await FollowupAsync($"Removed {purgeList.Count} messages from {user.Mention} in last {duration} before terminating due to too many messages (I only look back about 5000)");
            }
            else
            {
                await FollowupAsync($"Removed {purgeList.Count} messages from {user.Mention} in last {duration}, I think that's everything");
            }
            

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await FollowupAsync(e.Message);
            throw;
        }
    }

    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("ban","Ban a user from this server")]
    public async Task Ban(IGuildUser target, string? reason = null)
    {
        await DeferAsync();
        if (target.GuildPermissions.ModerateMembers)
        {
            await FollowupAsync("Can't ban moderators");
            return;
        }

        await _data.AddModlog(Context.User, target, ModLog.ModAction.Ban, reason);
        await _data.SaveChangesAsync();
        
        var dms = await target.CreateDMChannelAsync();
        if (dms != null)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle($"You have been banned from {target.Guild.Name}");
            if (reason != null) eb.WithDescription(reason);
            eb.WithColor(255,0,0);
            await dms.SendMessageAsync(embed: eb.Build());
        }
        await target.BanAsync();
        
        var eb2 = new EmbedBuilder();
        eb2.WithDescription(reason != null
            ? $"**{target.Username} banned** | {reason}"
            : $"**{target.Username} banned**");
        eb2.WithFooter($"{target.Id}");
        eb2.WithColor(255, 0, 0);

        await FollowupAsync(embed: eb2.Build());
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("kick","Kick a user from this server")]
    public async Task Kick(IGuildUser target, string? reason = null)
    {
        await DeferAsync();
        if (target.GuildPermissions.ModerateMembers)
        {
            await FollowupAsync("Can't kick moderators");
            return;
        }

        await _data.AddModlog(Context.User, target, ModLog.ModAction.Kick, reason);
        await _data.SaveChangesAsync();
        
        var dms = await target.CreateDMChannelAsync();
        if (dms != null)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle($"You have been kicked from {target.Guild.Name}");
            if (reason != null) eb.WithDescription(reason);
            eb.WithColor(255,0,0);
            await dms.SendMessageAsync(embed: eb.Build());
        }
        await target.KickAsync();
        
        var eb2 = new EmbedBuilder();
        eb2.WithDescription(reason != null
            ? $"**{target.Username} kicked** | {reason}"
            : $"**{target.Username} kicked**");
        eb2.WithFooter($"{target.Id}");
        eb2.WithColor(255, 0, 0);

        await FollowupAsync(embed: eb2.Build());
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("unban","Unban a user from this server")]
    public async Task Unban(IUser target, string? reason = null)
    {
        await DeferAsync();
        var ban = await Context.Guild.GetBanAsync(target);

        if (ban == null)
        {
            await FollowupAsync($"Doesn't look like {target.Username} is banned?");
            return;
        }
        
        await _data.AddModlog(Context.Guild.Id,Context.User.Id,target.Id, ModLog.ModAction.Unban, reason);
        await _data.SaveChangesAsync();
        
        var dms = await target.CreateDMChannelAsync();
        if (dms != null)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle($"You have been unbanned from {Context.Guild.Name}");
            if (reason != null) eb.WithDescription(reason);
            eb.WithColor(0,255,0);
            await dms.SendMessageAsync(embed: eb.Build());
        }
        
        await Context.Guild.RemoveBanAsync(target);
        var eb2 = new EmbedBuilder();
        eb2.WithDescription(reason != null
            ? $"**{target.Username} unbanned** | {reason}"
            : $"**{target.Username} unbanned**");
        eb2.WithFooter($"{target.Id}");
        eb2.WithColor(0, 255, 0);

        await FollowupAsync(embed: eb2.Build());
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("unmute","Unmute a user")]
    public async Task Unmute(IGuildUser target, string? reason = null)
    {
        await DeferAsync();

        if (target.TimedOutUntil == null || target.TimedOutUntil < DateTimeOffset.Now)
        {
            await FollowupAsync($"Doesn't look like {target.Username} is muted?");
            return;
        }

        await target.RemoveTimeOutAsync();
        await _data.AddModlog(Context.User,target, ModLog.ModAction.Unmute, reason);
        await _data.SaveChangesAsync();
        
        var eb2 = new EmbedBuilder();
        eb2.WithDescription(reason != null
            ? $"**{target.Username} unmuted** | {reason}"
            : $"**{target.Username} unmuted**");
        eb2.WithFooter($"{target.Id}");
        eb2.WithColor(0, 255, 0);
        
        await FollowupAsync(target.Mention,embed: eb2.Build());
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("mute","Unmute a user")]
    public async Task Mute(IGuildUser target, float time, string? reason = null, Unit unit = Unit.Minutes)
    {
        await DeferAsync();

        if (target.GuildPermissions.ModerateMembers)
        {
            await FollowupAsync("Can't mute moderators");
            return;
        }

        TimeSpan duration;
        string ustring;
        switch (unit)
        {
            case Unit.Minutes:
                duration = TimeSpan.FromMinutes(time);
                ustring = "minutes";
                break;
            case Unit.Hours:
                duration = TimeSpan.FromHours(time);
                ustring = "hours";
                break;
            case Unit.Days:
                duration = TimeSpan.FromDays(time);
                ustring = "days";
                break;
            default:
                duration = TimeSpan.FromMinutes(time);
                ustring = "minutes";
                break;
        }

        if (duration > TimeSpan.FromDays(14))
        {
            await FollowupAsync("Can't mute for longer than 14 days (Discord API limitation 🙄)");
            return;
        }

        await target.SetTimeOutAsync(duration);
        
        await _data.AddModlog(Context.User,target, ModLog.ModAction.Mute, $"{duration.TotalMinutes:n0}m {reason}");
        await _data.SaveChangesAsync();
        
        var eb2 = new EmbedBuilder();
        eb2.WithDescription(reason != null
            ? $"**{target.Username} muted for {time:n0} {ustring}** | {reason}"
            : $"**{target.Username} muted for {time:n0} {ustring}**");
        eb2.WithFooter($"{target.Id}");
        eb2.WithColor(255, 0, 0);
        
        await FollowupAsync(target.Mention,embed: eb2.Build());
    }

    public enum Unit
    {
        Minutes,
        Hours,
        Days
    }


    [MessageCommand("Report")]
    public async Task Flag(IMessage msg)
    {
        bool x = await _autoMod.Flag(msg,Context.Guild.Id,AutoMod.FlagReason.REPORT,$"Flagged by {Context.User.Username}");
        if(x) await RespondAsync("Report Submitted to Moderators", ephemeral: true);
        await RespondAsync("Failed. Admin probably hasn't set this up.", ephemeral: true);
    }
    
}