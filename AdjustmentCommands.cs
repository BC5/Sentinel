using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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

    [SlashCommand("frenchchannel","Channel where french will be enforced")]
    public async Task FrenchChannel([ChannelTypes(ChannelType.Text)] IGuildChannel channel)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        if (srv.FrenchChannel == channel.Id)
        {
            srv.FrenchChannel = null;
            await RespondAsync($"French Channel is now disabled");
        }
        else
        {
            srv.FrenchChannel = channel.Id;
            await RespondAsync($"French Channel is now <#{channel.Id}>");
        }
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
    
    [SlashCommand("frenchcost","Change cost of making someone speak french")]
    public async Task FrenchCost(int cost)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.FrenchCost = cost;
        await RespondAsync($"French Cost is now £{cost:n0}");
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

    [SlashCommand(name: "exportcfg", description: "Export this server's configuration to json")]
    public async Task Export()
    {
        await DeferAsync();
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        byte[] json = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(srv));
        var stream = new MemoryStream(json);
        await FollowupWithFileAsync(stream,$"{Context.Guild.Id}.json");
    }

    [SlashCommand(name: "importcfg", description: "Import a configuration from json")]
    public async Task ImportCfg(string url)
    {
        await DeferAsync();
        Uri uri = new Uri(url);
        switch (uri.Host)
        {
            case "cdn.discordapp.com":
            case "gist.githubusercontent.com":
            case "raw.githubusercontent.com":
                break;
            default:
                await FollowupAsync("I only accept json files served from Discord or Github servers");
                return;
        }
        HttpClient http = new HttpClient();
        var response = await http.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
        {
            await FollowupAsync($"Error getting file: {(int) response.StatusCode}");
            return;
        }

        string resp = await response.Content.ReadAsStringAsync();
        try
        {
            ServerConfig? srv = JsonSerializer.Deserialize<ServerConfig>(resp);
            if (srv == null)
            {
                await FollowupAsync("Error. Null after parsing.");
                return;
            }
            var data = _core.GetDbContext();
            await data.SetServerConfig(srv);
            await data.SaveChangesAsync();
            //Deduplicate
            var nsrv = await data.GetServerConfig(srv.DiscordID);
            nsrv.DeduplicateQuotes();
            await data.SaveChangesAsync();
            await FollowupAsync("Done.");
        }
        catch (Exception e)
        {
            await FollowupAsync($"Error parsing json: {e.Message}");
            return;
        }

    }
    
    [SlashCommand(name: "importquotes", description: "Import quotes from a json array")]
    public async Task ImportQuotes(string url)
    {
        await DeferAsync();
        Uri uri = new Uri(url);
        switch (uri.Host)
        {
            case "cdn.discordapp.com":
            case "gist.githubusercontent.com":
            case "raw.githubusercontent.com":
                break;
            default:
                await FollowupAsync("I only accept json files served from Discord or Github servers");
                return;
        }
        HttpClient http = new HttpClient();
        var response = await http.GetAsync(uri);
        if (!response.IsSuccessStatusCode)
        {
            await FollowupAsync($"Error getting file: {(int) response.StatusCode}");
            return;
        }

        string resp = await response.Content.ReadAsStringAsync();
        try
        {
            string[]? quotes = JsonSerializer.Deserialize<string[]>(resp);
            if (quotes == null)
            {
                await FollowupAsync("Error. Null after parsing.");
                return;
            }
            var data = _core.GetDbContext();
            var srv = await data.GetServerConfig(Context.Guild.Id);
            foreach (var q in quotes)
            {
                srv.Quotes.Add(new QuoteEntry() {ServerId = Context.Guild.Id,Text = q});
            }
            await data.SaveChangesAsync();
            await FollowupAsync("Done.");
        }
        catch (Exception e)
        {
            await FollowupAsync($"Error parsing json: {e.Message}");
            return;
        }

    }

    [SlashCommand(name: "addresponse", description: "Add an AutoResponse")]
    public async Task AddResponse()
    {
        await Context.Interaction.RespondWithModalAsync<ARModal>("new_response");
    }
    
    [SlashCommand(name: "removeresponse", description: "Remove an AutoResponse")]
    public async Task RemoveResponse(int page = 0)
    {
        try
        {
            var data = _core.GetDbContext();
            ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
            List<AutoResponse> autoResponses = srv.AutoResponses;

            var smb = new SelectMenuBuilder();

            bool more = false;
        
            for (int i = (page * 25); i < autoResponses.Count; i++)
            {
                var ar = autoResponses[i];
                if (smb.Options.Count == 25)
                {
                    more = true; 
                    break;
                }
                smb.AddOption(Truncate(ar.Trigger, 30), $"{ar.ResponseId}", $"{Truncate((ar.ResponseEmote + ar.ResponseText), 50)}");
            }

            if (smb.Options.Count == 0)
            {
                await RespondAsync("Nothing found to delete");
            }

            smb.WithMaxValues(1);
            smb.WithMinValues(1);
            smb.WithCustomId("del_response");
            
            var cb = new ComponentBuilder();
            cb.WithSelectMenu(smb);

            string moremsg = "";
            if (more) moremsg = $"\nThere's another page. Use `/adjust removeresponse {page + 1}` to see it";
            await RespondAsync($"**Select a response to delete.**{moremsg}", components: cb.Build());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }

    [ComponentInteraction("del_response",ignoreGroupNames:true)]
    public async Task RemoveResponseComponent(string selection)
    {
        int arid = int.Parse(selection);
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        var ar = srv.AutoResponses.FirstOrDefault(ar => ar.ResponseId == arid);
        if (ar != null)
        {
            srv.AutoResponses.Remove(ar);
            await RespondAsync($"Removed Response with Trigger {ar.Trigger}");
            await data.SaveChangesAsync();
        }
        else
        {
            await RespondAsync("Error: Couldn't find that response. Maybe it's already deleted?");
        }
    }

    private static string Truncate(string? str, int max)
    {
        if (str == null) return "";
        return str.Substring(0, Math.Min(str.Length, max));
    }

    [ModalInteraction("new_response", ignoreGroupNames:true)]
    public async Task ModalResponse(ARModal modal)
    {
        if (modal.ResponseText == "" && modal.ResponseEmote == "")
        {
            await RespondAsync("Error: You must enter some form of response");
            return;
        }
        
        AutoResponse r = new AutoResponse();
        r.Trigger = modal.Trigger.ToUpper();

        if (!int.TryParse(modal.Chance, out int chance) || chance > 100 || chance < 0)
        {
            await RespondAsync("Error: Your chance needs to be an integer 0-100");
            return;
        }
        r.Chance = chance;

        if (modal.ResponseText != "") r.ResponseText = modal.ResponseText;
        if (modal.ResponseEmote != "")
        {
            bool isemote = false;
            bool isemoji = false;
            if (Emoji.TryParse(modal.ResponseEmote, out var emoji))
            {
                isemoji = true;
            }
            else if (Emote.TryParse(modal.ResponseEmote, out var emote))
            {
                isemote = true;
            }

            if (!isemoji && !isemote)
            {
                await RespondAsync(
                    "Error: Failed to parse emote. I need a unicode emoji or a discord emote (i.e. in form `<:kirbpeter:852602866958073917>`)");
                return;
            }

            r.ResponseEmote = modal.ResponseEmote;
        }

        if (modal.RateLimit != "")
        {
            if (!int.TryParse(modal.RateLimit, out int rl))
            {
                await RespondAsync("Error: Didn't recognise your ratelimit time. Give it in seconds, as an integer");
                return;
            }

            r.RateLimit = true;
            r.ReloadTime = TimeSpan.FromSeconds(rl);
        }

        /*
        if (modal.UserTarget != null)
        {
            if (!ulong.TryParse(modal.UserTarget, out ulong ut))
            {
                await RespondAsync("Error: User target isn't a valid snowflake");
                return;
            }
            r.TargetUser = ut;
        }
        
    
        if (modal.ChannelTarget != null)
        {
            if (!ulong.TryParse(modal.ChannelTarget, out ulong ct))
            {
                await RespondAsync("Error: Channel isn't a valid snowflake");
                return;
            }
            r.TargetChannel = ct;
        }
        */
        
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);
        srv.AutoResponses.Add(r);
        await data.SaveChangesAsync();
        await RespondAsync("Added");
    }

    public class ARModal : IModal
    {
        public string Title => "Add an AutoResponse";
        
        [InputLabel("Trigger")]
        [ModalTextInput("trigger")]
        [RequiredInput(true)]
        public string Trigger { get; set; }

        [InputLabel("Response Text")] 
        [ModalTextInput("responsetext", TextInputStyle.Paragraph)] 
        [RequiredInput(false)]
        public string ResponseText { get; set; }
        
        [InputLabel("React with Emote")] 
        [ModalTextInput("responseemote", TextInputStyle.Short)] 
        [RequiredInput(false)]
        public string ResponseEmote { get; set; }

        [InputLabel("Chance of triggering (0-100)")]
        [ModalTextInput("chance", initValue: "100")]
        [RequiredInput(true)]
        public string Chance { get; set; } = "100";

        [InputLabel("Ratelimit")]
        [ModalTextInput("ratelimit",placeholder:"Time in seconds between responses")]
        [RequiredInput(false)]
        public string RateLimit { get; set; }
    }



}