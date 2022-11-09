using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Primitives;

namespace Sentinel;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("adjust","Commands to adjust parameters")]
public class AdjustmentCommands : InteractionModuleBase
{
    private Sentinel _core;
    public AdjustmentCommands(Sentinel core)
    {
        _core = core;
    }
    
    [SlashCommand("flagchannel","Channel where reports will be flagged")]
    public async Task FlagChannel([ChannelTypes(ChannelType.Text)] IGuildChannel channel)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.FlagChannel = channel.Id;
        await RespondAsync($"Flag Channel is now <#{channel.Id}>");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("modrole","Adjust role recognised as moderators")]
    public async Task Modrole(IRole role)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.ModRole = role.Id;
        await RespondAsync($"Mod role is now <@&{role.Id}>");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("idiotrole","Change role for idiots")]
    public async Task Idiotrole(IRole role)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.IdiotRole = role.Id;
        await RespondAsync($"Idiot role is now <@&{role.Id}>");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("idiotsentence","Adjust default idiot sentence")]
    public async Task IdiotSentence(int days)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.DefaultSentence = TimeSpan.FromDays(days);
        await RespondAsync($"Default sentence is now {days} days");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("mutecost","Change cost of mute")]
    public async Task MuteCost(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.MuteCost = cost;
        await RespondAsync($"Mute Cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("1984cost","Change cost of applying censor")]
    public async Task Cost1984(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.Cost1984 = cost;
        await RespondAsync($"1984 Cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("de1984cost","Change cost of removing censor")]
    public async Task Costde1984(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.CostDe1984 = cost;
        await RespondAsync($"De1984 Cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("nickcost","Change cost of nicklock")]
    public async Task NicklockCost(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.NickCost = cost;
        await RespondAsync($"Nicklock Cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("rewardsize","Change size of random reward")]
    public async Task RewardSize(int size)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.RewardSize = size;
        await RespondAsync($"Reward Size is now £{size:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("warncost","Change cost of /warn")]
    public async Task WarnCost(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.CostWarn = cost;
        await RespondAsync($"Warn cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("inflation","Adjust all prices by a %")]
    public async Task Inflate(float percent)
    {
        var data = _core.GetDbContext();
        percent = percent + 1;
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.CostWarn = (int) (srv.CostWarn * percent);
        srv.Cost1984 = (int) (srv.Cost1984 * percent);
        srv.CostDe1984 = (int) (srv.CostDe1984 * percent);
        srv.DeflectorCost = (int) (srv.DeflectorCost * percent);
        srv.MuteCost = (int) (srv.MuteCost * percent);
        srv.NickCost = (int) (srv.NickCost * percent);
        
        await RespondAsync($"Prices adjusted by {percent-1:P}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("deflector","Change cost of /warn")]
    public async Task DeflectorCost(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.DeflectorCost = cost;
        await RespondAsync($"Deflector cost is now £{cost:n0}");
        await data.SaveChangesAsync();
    }

    [SlashCommand("rewardchance","Change random chance of rewards")]
    public async Task RewardChance([MaxValue(1.0),MinValue(0.0)] double chance)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.RewardChance = (float) chance;
        await RespondAsync($"Reward Chance is now {(chance*100):n1}%");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand("usermultiplier","Change user's reward multiplier")]
    public async Task UserMultiplier(IGuildUser user, [MinValue(0.0),MaxValue(10.0)]double multiplier)
    {
        var data = _core.GetDbContext();
        if(multiplier < 0) return;
        ServerUser usr = await data.GetServerUser(Context.Guild.Id, user.Id);
        usr.Multiplier = (float) multiplier;
        await RespondAsync($"{user.Mention}'s Reward Multiplier is now {multiplier:n1}x");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand(name: "funnycommands", description: "Toggle the abuse commands off while still collecting data to update user balances")]
    public async Task FunnyCommands()
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.FunnyCommands = !srv.FunnyCommands;
        await RespondAsync($"Funny commands {(srv.FunnyCommands ? "enabled" : "disabled")}");
        await data.SaveChangesAsync();
    }
    
    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "authuser", description: "Toggle user's status as Authoritative")]
    public async Task AuthoritativeUser(IGuildUser user)
    {
        var data = _core.GetDbContext();
        ServerUser usr = await data.GetServerUser(user);
        usr.Authoritative = !usr.Authoritative;
        await RespondAsync($"{user.Mention} is now {(usr.Authoritative ? "authoritative" : "a Pleb")}");
        await data.SaveChangesAsync();
    }
    
    [SlashCommand(name: "immunity", description: "Toggle user's status as immune to slap commands")]
    public async Task ChangeImmunity(IGuildUser user)
    {
        var data = _core.GetDbContext();
        ServerUser usr = await data.GetServerUser(user);
        usr.Immune = !usr.Immune;
        await RespondAsync($"{user.Mention} {(usr.Immune ? "now" : "no longer")} has immunity");
        await data.SaveChangesAsync();
    }

    [SlashCommand(name: "attitude", description: "Set attitude Sentinel will take to a user")]
    public async Task SetAttitude(IGuildUser user, ServerUser.Attitude attitude)
    {
        var data = _core.GetDbContext();
        ServerUser usr = await data.GetServerUser(user);
        usr.SentinelAttitude = attitude;
        await RespondAsync($"I'll now treat {user.Mention} {attitude}");
        await data.SaveChangesAsync();
    }



}