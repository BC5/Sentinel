using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;

namespace Sentinel;

public class SlapCommands : InteractionModuleBase
{
    private Sentinel _core;
    private Data _data;
    
    public SlapCommands(Sentinel core, Data data)
    {
        _core = core;
        _data = data;
    }

    [SlashCommand("buydeflector",description:"Buy a deflector for mutes and nicklocks")]
    public async Task Deflect()
    {
        await DeferAsync(ephemeral: true);
        ServerUser usr = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        bool expired = true;
        if (usr.DeflectorExpiry != null)
        {
            expired = usr.DeflectorExpiry < DateTime.Now;
        }

        if (!expired)
        {
            await FollowupAsync(
                $"You have an active deflector. It will expire <t:{((DateTimeOffset) usr.DeflectorExpiry.Value).ToUnixTimeSeconds()}:R>");
            return;
        }

        if (usr.Balance < srv.DeflectorCost)
        {
            await FollowupAsync($"That costs £{srv.DeflectorCost:n0}. You're too poor.");
            return;
        }

        var status = await _data.Transact(usr, null, srv.DeflectorCost, Transaction.TxnType.Purchase);
        if (status != Transaction.TxnStatus.Success)
        {
            await FollowupAsync($"TXN ERROR: {status}");
            return;
        }
        usr.DeflectorExpiry = DateTime.Now + TimeSpan.FromHours(6);
        await _data.SaveChangesAsync();
        await FollowupAsync(
            $"You have purchased a deflector for £{srv.DeflectorCost:n0}.\nIt will expire <t:{((DateTimeOffset) usr.DeflectorExpiry.Value).ToUnixTimeSeconds()}:R>");
        return;
        
    }

    [SlashCommand("warn",description:"Tell someone off and add a note to their permanent record")]
    public async Task Warn(IGuildUser user, string warnreason)
    {
        ServerUser warner = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (warner.Balance < srv.CostWarn)
        {
            await RespondAsync($"Insufficient funds: That costs £{srv.CostWarn:n0}");
            return;
        }

        var status = await _data.Transact(warner, null, srv.CostWarn, Transaction.TxnType.Purchase);

        if (status != Transaction.TxnStatus.Success)
        {
            await RespondAsync($"TXN ERROR: {status}");
            return;
        }
        
        ServerWarns warn = new ServerWarns()
        {
            warned = user.Id,
            warner = Context.User.Id,
            serverid = Context.Guild.Id,
            warnReason = warnreason,
            warnTime = DateTime.Now
        };
        _data.Warns.Add(warn);

        var embed = new EmbedBuilder();
        embed.WithColor(Color.Green);
        embed.WithDescription($"✅ {user.Mention} has been warned. || {warnreason}");
        
        await _data.SaveChangesAsync();
        await RespondAsync(user.Mention,embed: embed.Build());
    }

    [SlashCommand("reducemute",description:"Take 5 minutes off someone's mute duration")]
    public async Task ReduceMute(IGuildUser targetuser, int multiplier = 1)
    {

        if (targetuser.TimedOutUntil == null || targetuser.TimedOutUntil < DateTimeOffset.Now)
        {
            await RespondAsync("They're not muted", ephemeral: true);
            return;
        }
        ServerUser user = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (multiplier < 1)
        {
            await RespondAsync($"Multiplier {multiplier} is stupid and you should feel bad");
            return;
        }
        
        if (user.Balance < srv.MuteCost*multiplier)
        {
            await RespondAsync($"That would cost £{srv.MuteCost*multiplier:n0} and you don't have enough", ephemeral: true);
            return;
        }

        await DeferAsync();
        
        var newtime = targetuser.TimedOutUntil - TimeSpan.FromMinutes(5*multiplier);

        var status = await _data.Transact(user, null, srv.MuteCost*multiplier, Transaction.TxnType.Purchase);
        await _data.SaveChangesAsync();

        if (status != Transaction.TxnStatus.Success)
        {
            await FollowupAsync("TXN ERROR: " + status);
            return;
        }
        
        if (newtime < DateTimeOffset.Now)
        {
            await targetuser.RemoveTimeOutAsync(new RequestOptions() {AuditLogReason = $"Courtesy of {Context.User.Username}"});
            await Context.Interaction.FollowupAsync(
                $"Ended {targetuser.Mention}'s mute\nCost:£{srv.MuteCost*multiplier:n0}", ephemeral: false);
            return;
        }
        
        var newtimespan = newtime - DateTimeOffset.Now;
        await targetuser.SetTimeOutAsync(newtimespan.Value,
            new RequestOptions() {AuditLogReason = $"Took {multiplier*5} minutes off, Courtesy of {Context.User.Username}"});
        await Context.Interaction.FollowupAsync(
            $"Took {multiplier*5} minutes off {targetuser.Mention}'s mute\nCost:£{srv.MuteCost*multiplier:n0}", ephemeral: false);
    }

    [SlashCommand("nicklock",description:"Change someone's nickname and stop them from changing it back")]
    public async Task NickChangeCommand(IGuildUser targetuser, string nickname)
    {
        var tolower = nickname.ToLower();
        if (tolower.Contains("nigg") || tolower.Contains("fag") || tolower.Contains("paki"))
        {
            Console.WriteLine($"{Context.User.Username} attempted to set a nick to {nickname}");
            await RespondAsync("BEHAVE. <@&1021889676870701119>");
            await ((IGuildUser) Context.User).SetTimeOutAsync(TimeSpan.FromMinutes(30));
            return;
        }
        
        ServerUser user = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerUser target = await _data.GetServerUser(targetuser.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (target.Immune)
        {
            await RespondAsync($"{targetuser.Mention} is immune (no fun allowed)");
            return;
        }
        
        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        
        if (user.Balance < srv.NickCost)
        {
            await Context.Interaction.RespondAsync($"You don't have the funds for that\nCost: £{srv.NickCost:n0}\nBalance: £{user.Balance:n0}", ephemeral: true);
            return;
        }

        var nicklock = target;
        IUser nicklocktarget = targetuser;
        bool deflected = false;
        
        if (target.ValidDeflector())
        {
            nicklock = user;
            nicklocktarget = Context.Interaction.User;
            deflected = true;
            target.DeflectorExpiry = null;
        }
        
        if (nicklock.NicklockUntil == null || nicklock.NicklockUntil < DateTime.Now)
        {
            nicklock.PrevNick = targetuser.Nickname;
        }
        nicklock.Nicklock = nickname;
        nicklock.NicklockUntil = DateTime.Now + TimeSpan.FromMinutes(15);
        if (nicklocktarget is SocketGuildUser sguilduser)
        {
            await sguilduser.ModifyAsync(properties => properties.Nickname = nickname);
        }
        else
        {
            Console.WriteLine("oopie");
        }

        var status = await _data.Transact(user, null, srv.NickCost, Transaction.TxnType.Purchase);
        await _data.SaveChangesAsync();
        if (deflected)
        {
            await Context.Interaction.RespondAsync($"{nicklocktarget.Mention} <:thake:1019347821100539916>\n" 
            + "You've got that nickname for 15 minutes unless you use `/nicklock` to lock it to something else" +
            $"\n({targetuser.Mention} had a deflector)");
        }
        await Context.Interaction.RespondAsync($"{targetuser.Mention} <:thake:1019347821100539916>\n" 
        + "You've got that nickname for 15 minutes unless you use `/nicklock` to lock it to something else");

    }

    [SlashCommand("francophone",description:"Parlez Français. C’est obligatoire.")]
    public async Task Francophone(IGuildUser target)
    {
        await DeferAsync();
        ServerUser u = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerUser t = await _data.GetServerUser(target.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (t.Immune)
        {
            await RespondAsync($"{target.Mention} is immune (no fun allowed)");
            return;
        }
        
        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        
        if (u.Balance < srv.FrenchCost)
        {
            await FollowupAsync($"You don't have the £{srv.FrenchCost:n0} required for that");
            return;
        }

        await _data.Transact(u, null, srv.FrenchCost, Transaction.TxnType.Purchase);
                    
        if (t.ValidDeflector())
        {
            t.DeflectorExpiry = null;
            u.Francophone = !u.Francophone;
            await _data.SaveChangesAsync();
            if(u.Francophone)
            {
                await FollowupAsync(
                $"<@{u.UserSnowflake}> parle vous français? Parce que tu dois parler français. ({target.Mention} avait un déflecteur)");
            }
            else
            {
                await FollowupAsync($"no more frogspeak for <@{u.UserSnowflake}>. this burnt a deflector. i couldn't be bothered to code it so it doesn't. cope {target.Mention}.");
            }
        }
        else
        {
            t.Francophone = !t.Francophone;
            await _data.SaveChangesAsync();
            if (t.Francophone) await FollowupAsync($"<@{t.UserSnowflake}> parle vous français? Parce que tu dois parler français.");
            else await FollowupAsync($"<@{t.UserSnowflake}> no more frogspeak 🥳");
        }
    }

    [UserCommand("Shut Up")]
    public async Task ShutUpContext(IGuildUser target)
    {
        await ShutUpSlash(target);
    }
    
    [SlashCommand("shutup",description:"Mute someone for 5 minutes")]
    public async Task ShutUpSlash(IGuildUser target, int multiplier = 1)
    {
        try
        {
            ServerUser user = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
            ServerUser targetsu = await _data.GetServerUser(target);
            ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);
            
            if (targetsu.Immune)
            {
                await RespondAsync($"{target.Mention} is immune (no fun allowed)");
                return;
            }
            
            if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}

            if (multiplier < 1)
            {
                await RespondAsync($"Multiplier {multiplier} is stupid and you should feel bad");
                return;
            }
            
            if (user.Balance < (srv.MuteCost*multiplier))
            {
                await Context.Interaction.RespondAsync($"You don't have the funds for that\nCost: £{srv.MuteCost*multiplier:n0}\nBalance: £{user.Balance:n0}", ephemeral: true);
                return;
            }

            await Context.Interaction.DeferAsync(ephemeral: false);

            bool deflected = false;
            var ogtarget = target;
            if (targetsu.ValidDeflector())
            {
                targetsu.DeflectorExpiry = null;
                (user, targetsu) = (targetsu, user);
                target = (IGuildUser) Context.User;
                deflected = true;
            }
            
            if (target.TimedOutUntil == null || target.TimedOutUntil < DateTimeOffset.Now)
            {
                await target.SetTimeOutAsync(TimeSpan.FromMinutes(5*multiplier),
                    new RequestOptions() {AuditLogReason = $"Courtesy of {Context.User.Username}"});
                if (deflected)
                {
                    await Context.Interaction.FollowupAsync($"Muted {target.Mention} for {5*multiplier} minutes ({ogtarget.Mention} had a deflector)\nCost:£{srv.MuteCost*multiplier:n0}", ephemeral: false);
                }
                else
                {
                    await Context.Interaction.FollowupAsync($"Muted {target.Mention} for {5*multiplier} minutes\nCost:£{srv.MuteCost*multiplier:n0}", ephemeral: false);
                }
            }
            else
            {
                var time = target.TimedOutUntil + TimeSpan.FromMinutes(5*multiplier);
                var newtimespan = time - DateTimeOffset.Now;
                await target.SetTimeOutAsync(newtimespan.Value,
                    new RequestOptions() {AuditLogReason = $"Courtesy of {Context.User.Username}"});
                await Context.Interaction.FollowupAsync(
                    $"Added {5*multiplier} more minutes to {target.Mention}'s mute <:troll:1019348578113699911>\nCost:£{srv.MuteCost*multiplier:n0}", ephemeral: false);
            }

            if (deflected)
            {
                var status = await _data.Transact(targetsu, null, srv.MuteCost*multiplier, Transaction.TxnType.Purchase);
            }
            else
            {
                var status = await _data.Transact(user, null, srv.MuteCost*multiplier, Transaction.TxnType.Purchase);
            }
            
            await _data.SaveChangesAsync();
        }
        catch (HttpException e)
        {
            if (e.DiscordCode == DiscordErrorCode.InsufficientPermissions)
            {
                await Context.Interaction.FollowupAsync("I don't have sufficient permissions to 1984 that user",
                    ephemeral: true);
            }

            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }
}