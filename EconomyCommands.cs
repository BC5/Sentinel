using System.Security.Cryptography;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

public class EconomyCommands : InteractionModuleBase
{
    
    private Sentinel _core;
    private Data _data;
    public EconomyCommands(Sentinel core, Data data)
    {
        _core = core;
        _data = data;
    }
    
    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "greatreset", description: "Turn the economy off and on again")]
    public async Task EconomyReset(bool hardreset = false)
    {
        ServerUser u = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        if (!u.Authoritative)
        {
            await Context.Interaction.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009");
            return;
        }
        
        await DeferAsync(ephemeral: true);
        try
        {
            List<Transaction> txns = await _data.Transactions.Where(x => x.ServerID == Context.Guild.Id).ToListAsync();
            _data.Transactions.RemoveRange(txns);
            await _data.SaveChangesAsync();
            List<ServerUser> users = await _data.Users.Where(x => x.ServerSnowflake == Context.Guild.Id).ToListAsync();
            if (hardreset)
            {
                foreach (var user in users)
                {
                    user.Balance = 0;
                }
                await _data.SaveChangesAsync();
            }
            else
            {
                ServerUser virtualUser = await _data.GetServerUser(Context.Guild.Id, Context.Guild.Id);
                virtualUser.Balance = 0;
                await _data.SaveChangesAsync();
            
                foreach (var user in users)
                {
                    if (!(user == virtualUser))
                    {
                        int i = user.Balance;
                        user.Balance = 0;
                        await _data.Transact(null, user, i, Transaction.TxnType.StartingBalance, true,true);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await FollowupAsync("Reset Errored");
            return;
        }
        await FollowupAsync("Reset Complete");
    }

    [SlashCommand(name: "leaderboard", description: "See whose got the most money")]
    public async Task Leaderboard(int count = 10)
    {
        var top10 = _data.GetTop(Context.Guild.Id,count);

        EmbedBuilder eb = new();
        eb.WithTitle("Top Users by Balance");
        eb.WithColor(0xFFF700);
        int i = 1;
        string leaderboard = "";
        foreach (var user in top10)
        {
            if (user.ServerSnowflake != user.UserSnowflake)
            {
                leaderboard = leaderboard + $"**{i}** - <@{user.UserSnowflake}> - £{user.Balance:n0}\n";
            }
            else
            {
                leaderboard = leaderboard + $"**{i}** - Server - £{user.Balance:n0}\n";
            }
            i++;
        }
        eb.WithDescription(leaderboard);

        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }
    
    [SlashCommand(name: "moneysupply", description: "Check cash in circulation")]
    public async Task MoneySupply()
    {
        ServerUser virtualUser = await _data.GetServerUser(Context.Guild.Id, Context.Guild.Id);
        await RespondAsync($"Money Supply: £{(0 - virtualUser.Balance):n0}",ephemeral:true);
    }

    [SlashCommand(name: "report", description: "Generate an economic report")]
    public async Task GenReport()
    {
        await DeferAsync(ephemeral:true);
        DateTime start = DateTime.Now;
        List<Transaction> transactions = await _data.Transactions.Where(x => x.ServerID == Context.Guild.Id).ToListAsync();

        int printed = 0;
        int created = 0;
        int unknown = 0;
        
        int taxed = 0;
        
        int outstanding = 0;
        
        int spent = 0;
        int burnt = 0;
        
        int transfers = 0;
        int theft = 0;

        foreach (var txn in transactions)
        {
            if (txn.SenderID == Context.Guild.Id)
            {
                //Server Virtual Account Money Creation
                if (txn.Reason == Transaction.TxnType.Print) printed = printed + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.RewardMessage) created = created + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.RewardReaction) created = created + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.Refund) spent = spent - txn.Amount;
                else if (txn.Reason == Transaction.TxnType.StartingBalance) unknown = unknown + txn.Amount;
                else
                {
                    //*shouldn't* happen
                    Console.WriteLine("A:"+txn.Reason+$" ({txn.Amount}) {txn.SenderID} -> {txn.RecipientID}");
                }
                outstanding = outstanding + txn.Amount;
            }
            else if (txn.RecipientID == Context.Guild.Id)
            {
                //Server Virtual Account Money Destruction
                if (txn.Reason == Transaction.TxnType.Purchase) spent = spent + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.Tax) taxed = taxed + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.Theft) burnt = burnt + txn.Amount;
                else if (txn.Reason == Transaction.TxnType.RewardReaction) created = created - txn.Amount;
                else if (txn.Reason == Transaction.TxnType.StartingBalance) unknown = unknown - txn.Amount;
                else
                {
                    //*shouldn't* happen
                    Console.WriteLine("B:"+txn.Reason);
                }
                outstanding = outstanding - txn.Amount;
            }
            else
            {
                switch (txn.Reason)
                {
                    case Transaction.TxnType.Transfer:
                        transfers = transfers + txn.Amount;
                        break;
                    case Transaction.TxnType.Theft:
                    case Transaction.TxnType.Seizure:
                        theft = theft + txn.Amount;
                        break;
                    default:
                        Console.WriteLine("C:" + txn.Reason);
                        break;
                }
            }
        }
        
        EmbedBuilder eb = new();
        eb.WithTitle("Economic Report");
        eb.WithColor(0xFFF700);
        int i = 1;
        string report = 
            $"Printed: £{printed:n0}\n" +
            $"Rewards: £{created:n0}\n" +
            $"Unknown: £{unknown:n0}\n" +
            $"**Total Money Created**: £{printed+created+unknown:n0}\n" +
            $"---\n" +
            $"Taxed: £{taxed:n0}\n" +
            $"Spent: £{spent:n0}\n" +
            $"Burnt: £{burnt:n0}\n" +
            $"**Total Money Destroyed**: £{taxed+spent+burnt:n0}\n" +
            $"---\n" +
            $"**In Circulation**: £{outstanding:n0}\n" +
            $"---\n" +
            $"Transfers: £{transfers:n0}\n" +
            $"Theft: £{theft:n0}\n" +
            $"**Transfer Volume**: £{transfers+theft:n0}\n" +
            "---\n" +
            $"**Total Volume**: £{printed+created+unknown+taxed+spent+burnt+transfers+theft:n0}";
        eb.WithDescription(report);
        var duration = DateTime.Now - start;
        eb.WithFooter($"Report Generated in {duration.TotalMilliseconds:n0}ms");
        if (unknown != 0)
        {
            await FollowupAsync("This economy has been soft reset at least once. \"Unknown\" will be present",embed: eb.Build());
            return;
        }
        await FollowupAsync(embed: eb.Build());
    }
    
    [SlashCommand(name: "steal", description: "Attempt a heist. £50 fee. You may lose everything.")]
    public async Task Theft(int amount)
    {
        var users = await Context.Guild.GetUsersAsync();
        int userindex = RandomNumberGenerator.GetInt32(users.Count+1);
        ServerUser thief = await _data.GetServerUser(Context.User.Id,Context.Guild.Id);
        if (userindex == users.Count)
        {
            await RespondAsync($"You just tried to steal from me. I'm taking everything.\n**{Context.User.Mention} lost £{thief.Balance:n0}**");
            Console.WriteLine("TakeAll Triggered: Type A");
            await _data.Transact(thief, null, thief.Balance, Transaction.TxnType.Theft);
            return;
        }
        var user = users.ElementAt(userindex);

        
        ServerUser victim = await _data.GetServerUser(user);
        
        if (victim.UserSnowflake == Context.Client.CurrentUser.Id)
        {
            await RespondAsync($"You just tried to steal from me. I'm taking everything.\n**{Context.User.Mention} lost £{thief.Balance:n0}**");
            Console.WriteLine("TakeAll Triggered: Type B");
            await _data.Transact(thief, null, thief.Balance, Transaction.TxnType.Theft);
            return;
        }

        if (amount <= 0)
        {
            await RespondAsync("Naughty. That would *probably* break my code 🙄");
            return;
        }

        if (thief.Balance < 50)
        {
            await RespondAsync("£50 transaction fee on all thefts. You don't have enough to cover that (yikes, povvo)");
            return;
        }
        
        await _data.Transact(thief, null, 50, Transaction.TxnType.Theft);
        
        if (thief.UserSnowflake == victim.UserSnowflake)
        {
            await RespondAsync($"Stole from themselves. Idiot\n**{Context.User.Mention} lost £{50}**");
            return;
        }
        
        if (amount > victim.Balance)
        {
            if (amount > thief.Balance)
            {
                amount = thief.Balance;
            }

            if (user.IsBot)
            {
                await RespondAsync($"{Context.User.Mention} tried to steal £{amount:n0} from {user.Username}. They didn't have enough and it backfired." +
                                   $"\n**{Context.User.Username} lost £{amount+50:n0}**\n**{user.Username} is a bot so £{amount:n0} goes to <@241325827810131978> instead** (my code my rules)");
                await _data.Transact(thief.UserSnowflake, 241325827810131978, Context.Guild.Id, amount, Transaction.TxnType.Theft);
            }
            else
            {
                await RespondAsync($"{Context.User.Mention} tried to steal £{amount:n0} from {user.Username}. They didn't have enough and it backfired." +
                                   $"\n**{Context.User.Username} lost £{amount+50:n0}**\n**{user.Username} gained £{amount:n0}**");
                await _data.Transact(thief, victim, amount, Transaction.TxnType.Theft);
            }
            return;
        }
        else
        {
            await RespondAsync($"{Context.User.Mention} stole £{amount:n0} from {user.Username}." +
                               $"\n**{Context.User.Username} gained £{amount-50:n0}**\n**{user.Username} lost £{amount:n0}**");
            await _data.Transact(victim, thief, amount, Transaction.TxnType.Theft);
            return;
        }

    }
    
    [SlashCommand(name: "stimulus", description: "Give cash to everyone in a role")]
    public async Task HelicopterMoney(IRole role, int amount)
    {
        ServerUser user = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (!user.Authoritative)
        {
            await Context.Interaction.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009");
            return;
        }

        await DeferAsync();
        
        var users = await Context.Guild.GetUsersAsync();

        List<Task<Transaction.TxnStatus>> tasks = new();

        int siezureamount = 0;
        
        foreach (var u in users)
        {

            if (u.RoleIds.Contains(role.Id))
            {
                if (amount >= 0)
                {
                    tasks.Add(_data.Transact(null, u.Id,Context.Guild.Id, amount, Transaction.TxnType.Print,allowDebt:true));
                }
                else
                {
                    int seize = 0-amount;
                    var profile = await _data.GetServerUser(u);
                    if (seize > profile.Balance)
                    {
                        seize = profile.Balance;
                    }
                    siezureamount = siezureamount + seize;
                    tasks.Add(_data.Transact(u.Id, null,Context.Guild.Id, seize, Transaction.TxnType.Tax,allowDebt:false,allowSeizure:true));
                }
            }
        }

        await Task.WhenAll(tasks);
        await _data.SaveChangesAsync();

        int successes = 0;
        int failures = 0;
        foreach (var task in tasks)
        {
            if (task.Result == Transaction.TxnStatus.Success) successes++;
            else failures++;
        }

        if (amount < 0)
        {
            if (failures == 0)
            {
                await Context.Interaction.FollowupAsync($"🔥️💷💷💷\n{successes} Transactions Succeeded\n{failures} Transactions Failed\n£{siezureamount:n0} Destroyed");
            }
            else
            {
                await Context.Interaction.FollowupAsync($"🔥️💷💷💷\n{successes} Transactions Succeeded\n{failures} Transactions Failed\nAmount destroyed uncertain due to txn failure. Potentially £{siezureamount:n0}");
            }
            return;
        }
        await Context.Interaction.FollowupAsync($"🖨️💷💷💷\n{successes} Transactions Succeeded\n{failures} Transactions Failed\n£{successes*amount:n0} Distributed");
    }
    
    [SlashCommand(name: "balance", description: "Check your balance")]
    public async Task Balance(IUser? target = null)
    {
        if (target == null) target = Context.User;

        try
        {
            await Context.Interaction.DeferAsync(ephemeral: true);
            ServerUser prof = await _data.GetServerUser(target.Id,Context.Guild.Id);
            await Context.Interaction.FollowupAsync($"{target.Mention}'s Balance: £{prof.Balance:n0}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    [SlashCommand(name: "print", description: "Quantitative Easing")]
    public async Task Print(int amount)
    {
        ServerUser user = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);

        if (!user.Authoritative)
        {
            await Context.Interaction.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009");
            return;
        }
        
        var status = await _data.Transact(null, user, amount, Transaction.TxnType.Print,allowDebt:true);
        Console.WriteLine(status);
        await _data.SaveChangesAsync();
        await Context.Interaction.RespondAsync("🖨️💷💷💷");
    }
    
    [SlashCommand(name: "transfer", description: "Give money to another user")]
    public async Task Transfer(IUser recipient, int amount)
    {
        if (amount == 0)
        {
            await Context.Interaction.RespondAsync("Well that's a bit pointless", ephemeral: true);
        }
        
        ServerUser recipi = await _data.GetServerUser(recipient.Id, Context.Guild.Id);
        ServerUser sender = await _data.GetServerUser(Context.User.Id, Context.Guild.Id);
        if (amount < 0 && !sender.Authoritative)
        {
            await Context.Interaction.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009");
            return;
        }

        if (amount < 0 && recipi.Balance - amount < 0)
        {
            await Context.Interaction.RespondAsync("You can't put them into debt, you monster.", ephemeral: true);
            return;
        }

        if (amount > 0 && sender.Balance - amount < 0)
        {
            await Context.Interaction.RespondAsync("You can't put yourself into debt.", ephemeral: true);
            return;
        }
        
        var status = await _data.Transact(sender, recipi, amount, allowDebt:sender.Authoritative, allowSeizure:sender.Authoritative);

        if (amount < 0)
        {
            await Context.Interaction.RespondAsync($"£{0-amount:n0} stole from {recipient.Mention}");
        }
        else
        {
            await Context.Interaction.RespondAsync($"£{amount:n0} transferred to {recipient.Mention}");
        }
        await _data.SaveChangesAsync();

    }
    
}