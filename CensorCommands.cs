using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Sentinel;

[Group("censor","Commands affecting the censor")]
public class CensorCommands : InteractionModuleBase
{
    private Sentinel _core;
    
    public CensorCommands(Sentinel core)
    {
        this._core = core;
    }

    [SlashCommand("1984","Apply the censor to a user")]
    public async Task Add1984(IGuildUser target)
    {
        await Set1984(target, true);
    }
    
    [SlashCommand("free","Free a user from censorship")]
    public async Task Take1984(IGuildUser target)
    {
        await Set1984(target, false);
    }

    public async Task Set1984(IGuildUser targett, bool on)
    {
        var data = _core.GetDbContext();
        ServerUser user = await data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerUser target = await data.GetServerUser(targett.Id, Context.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        if (targett.IsBot) {await RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009", ephemeral: true); return;}
        
        int cost = on ? srv.Cost1984 : srv.CostDe1984;
        
        if (user.Balance < cost)
        {
            await RespondAsync($"Insufficient funds : That requires £{cost:n0}",ephemeral: true);
            return;
        }
        if (target.Censored == on)
        {
            await RespondAsync($"They're already {(on?"censored":"free")}",ephemeral: true);
            return;
        }

        target.Censored = on;
        var status = await data.Transact(user, null, cost, Transaction.TxnType.Purchase);

        if (status != Transaction.TxnStatus.Success)
        {
            Console.WriteLine("txn failed: " + status);
            return;
        }
        
        if (on) await RespondAsync($"{targett.Mention} 1984");
        else await RespondAsync($"{targett.Mention} Free Speech Prevails");

        await data.SaveChangesAsync();
    }

    [SlashCommand("check","See the current rules")]
    public async Task CheckCensor(bool ephemeral = true)
    {
        var data = _core.GetDbContext();
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        string msg = "**State of the Censor**";
        foreach (CensorEntry entry in srv.Censor)
        {
            msg = msg + $"\nYou must{(entry.Requirement ? " " : " not ")}say: `{entry.Phrase}` {(entry.Wildcard?"\\*":"")}";
        }
        await RespondAsync(msg, ephemeral: ephemeral);
    }
    
    [SlashCommand("blacklist","Add a no-no word to the censor")]
    public async Task AddBlacklist(string phrase, bool wildcard = false)
    {
        var data = _core.GetDbContext();
        phrase = phrase.ToUpper();
        ServerUser user = await data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        
        var cost = 5000 / (phrase.Length + 4) + 25;

        if (wildcard)
        {
            cost = (int) (cost * 1.20);
        }

        if (cost > user.Balance)
        {
            await RespondAsync($"Insufficient funds : That requires £{cost:n0}\n" +
                               $"Try a longer phrase, shorter phrases cost more to 1984",ephemeral: true);
            return;
        }

        var status = await data.Transact(user, null, cost, Transaction.TxnType.Purchase);

        CensorEntry censor = new CensorEntry
        {
            Phrase = phrase,
            Requirement = false,
            Wildcard = wildcard
        };

        srv.Censor.Add(censor);
        await data.SaveChangesAsync();
        await RespondAsync($"1984'd phrase {phrase}, That'll be £{cost:n0} please.");
    }

    [SlashCommand("remove","Remove a phrase from the censor")]
    public async Task Decensor(string phrase, bool whitelist = false)
    {
        var data = _core.GetDbContext();
        phrase = phrase.ToUpper();
        ServerUser user = await data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        
        if (srv.CostDe1984 > user.Balance)
        {
            await RespondAsync($"Insufficient funds : That requires £{srv.CostDe1984:n0}",ephemeral: true);
            return;
        }

        bool found = false;
        foreach (var censor in srv.Censor)
        {
            if (censor.Phrase == phrase && censor.Requirement == whitelist)
            {
                found = true;
                srv.Censor.Remove(censor);
                break;
            }
        }

        if (!found)
        {
            await RespondAsync($"Can't remove something that's not there", ephemeral: true);
            return;
        }
        await RespondAsync($"{phrase} is no longer on the {(whitelist?"whitelist":"blacklist")} 🥳");
        
        var status = await data.Transact(user, null, srv.CostDe1984, Transaction.TxnType.Purchase);
        await data.SaveChangesAsync();

    }
    
    [SlashCommand("whitelist","Add a no-no word to the censor")]
    public async Task AddWhitelist(string phrase, bool wildcard = true)
    {
        var data = _core.GetDbContext();
        phrase = phrase.ToUpper();
        ServerUser user = await data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(Context.Guild.Id);

        if (!srv.FunnyCommands) {await RespondAsync("Disabled by Admin", ephemeral: true); return;}
        
        var cost = (int) (0.4 * Math.Pow(phrase.Length, 2) + 100)*5;

        if (wildcard)
        {
            cost = (int) (cost * 0.90);
        }
        
        if (cost > user.Balance)
        {
            await RespondAsync($"Insufficient funds : That requires £{cost:n0}\n" +
                               $"Try a shorter phrase, longer phrases cost more to force people to say",ephemeral: true);
            return;
        }

        var status = await data.Transact(user, null, cost, Transaction.TxnType.Purchase);
        
        CensorEntry censor = new CensorEntry
        {
            Phrase = phrase,
            Requirement = true,
            Wildcard = wildcard
        };
        
        srv.Censor.Add(censor);
        await data.SaveChangesAsync();

        await RespondAsync($"1984'd phrase {phrase}, That'll be £{cost} please.");

    }
}