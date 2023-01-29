using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Transactions;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

public class Data : DbContext
{

    public DbSet<ServerConfig> Servers { get; set; }
    public DbSet<ServerUser> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<ServerWarns> Warns { get; set; }
    public DbSet<Reaction> ReactLog { get; set; }
    public DbSet<OCREntry> OcrEntries { get; set; }

    [NotMapped]
    public string DbPath { get; }
    
    public Data()
    {
        
    }
    
    public Data(string file)
    {
        DbPath = file;
    }

/*
    public async Task TopByReact(string emote, ulong server)
    {
        var result = ReactLog
            .FromSqlInterpolated(
                $"SELECT *, count() FROM ReactLog WHERE ServerId = {server} AND ReactName = {emote} GROUP BY MessageId ORDER BY count() DESC;")
            .ToListAsync();
        
        this.Database

        Console.WriteLine("bbbbb");
        return; 
    }
*/

    public async Task<bool> CheckVoted(ulong poll, ulong user)
    {
        var results = await Votes.Where(x => x.Poll == poll && x.User == user).ToListAsync();
        return results.Count > 0;
    }

    public async Task RecordVote(ulong poll, ulong user)
    {
        await Votes.AddAsync(new Vote() {Poll = poll, User = user});
        await SaveChangesAsync();
    }
    
    public List<ServerUser> GetTop(ulong server, int quantity = 10)
    {
        var results = Users.Where(x => x.ServerSnowflake == server)
            .OrderByDescending(x => x.Balance).Take(quantity).ToList();
        return results;
    }
    
    public async Task<Transaction.TxnStatus> Transact(ulong? sender, ulong? recipient, ulong server, int amount, Transaction.TxnType type = Transaction.TxnType.Transfer, bool allowDebt = false, bool allowSeizure = false)
    {
        ServerUser? rx = null, tx = null;
        if(recipient.HasValue) rx = await GetServerUser(recipient.Value, server);
        if(sender.HasValue) tx = await GetServerUser(sender.Value, server);
        return await Transact(tx, rx, amount, type, allowDebt, allowSeizure);
    }
    
    public async Task<Transaction.TxnStatus> Transact(ServerUser? sender, ServerUser? recipient, int amount, Transaction.TxnType type = Transaction.TxnType.Transfer, bool allowDebt = false, bool allowSeizure = false)
    {
        if (recipient == null && sender == null) return Transaction.TxnStatus.Invalid;
        if (recipient == null && sender != null)
        {
            recipient = await GetServerUser(sender.ServerSnowflake,sender.ServerSnowflake);
        }
        else if (sender == null && recipient != null)
        {
            sender = await GetServerUser(recipient.ServerSnowflake, recipient.ServerSnowflake);
        }

        if (recipient == null) return Transaction.TxnStatus.Failed;
        if (sender == null) return Transaction.TxnStatus.Failed;
        
        bool siezed = false;
        if (amount < 0)
        {
            (sender, recipient) = (recipient, sender);
            amount = 0 - amount;
            if (type == Transaction.TxnType.Transfer) type = Transaction.TxnType.Seizure;
            if (!allowSeizure) return Transaction.TxnStatus.Invalid;
            siezed = true;
        }
        if (!allowDebt && sender.Balance < amount) return Transaction.TxnStatus.InsufficientFunds;

        Transaction t = new()
        {
            Amount = amount,
            Reason = type,
            ServerID = sender.ServerSnowflake,
            RecipientID = recipient.UserSnowflake,
            SenderID = sender.UserSnowflake
        };

        sender.Balance -= amount;
        recipient.Balance += amount;

        if (type is Transaction.TxnType.RewardMessage or Transaction.TxnType.RewardReaction)
        {
            if (siezed) sender.Earnings -= amount;
            else recipient.Earnings += amount;
        }

        Transactions.Add(t);
        await SaveChangesAsync();
        return Transaction.TxnStatus.Success;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder opt)
    {
        opt.UseSqlite($"Data Source={DbPath}");
    }

    public async Task<ServerUser> GetServerUser(ulong user, ulong server)
    {
        var results = await Users.Where(x => x.UserSnowflake == user && x.ServerSnowflake == server).ToListAsync();
        if (results.Count == 0)
        {
            try
            {
                var newuser = new ServerUser(user,server);
                Users.Add(newuser);
                await SaveChangesAsync();
                return newuser;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        return results[0];
    }
    
    public async Task<ServerConfig> GetServerConfig(ulong server)
    {
        var results = await Servers.Where(x => x.DiscordID == server)
            .Include(y => y.Censor)
            .Include(y => y.AutoResponses)
            .Include(y => y.Quotes)
            .ToListAsync();
        if (results.Count == 0)
        {
            var newserver = new ServerConfig(server);
            Servers.Add(newserver);
            return newserver;
        }
        return results[0];
    }

    public async Task SetServerConfig(ServerConfig srv)
    {
        var results = await Servers.Where(x => x.DiscordID == srv.DiscordID).Include(y => y.Censor)
            .Include(y => y.AutoResponses).ToListAsync();
        if (results.Count != 0)
        {
            Servers.Remove(results[0]);
        }
        Servers.Add(srv);
    }
    
    public async Task<ServerUser> GetServerUser(IGuildUser user)
    {
        return await GetServerUser(user.Id, user.GuildId);
    }

    public async void AddTxn(Transaction txn)
    {
        Transactions.Add(txn);
    }

    public async Task AddReact(SocketReaction r, IMessage msg)
    {
        //Console.WriteLine("Adding");
        ReactLog.Add(new Reaction(r, msg));
        await SaveChangesAsync();
    }

    public async Task RemoveReact(SocketReaction r, IMessage msg)
    {
        List<Reaction> re = (await ReactLog
            .Where(x => x.MessageId == msg.Id && x.ReactorId == r.UserId && x.ReactName == r.Emote.Name)
            .ToListAsync());
        if (re.Count > 0)
        {
            //Console.WriteLine("Removing React Entry");
            ReactLog.Remove(re[0]);
            await SaveChangesAsync();
        }
    }
    
}

public class ServerUser
{
    public ServerUser(ulong uid, ulong sid)
    {
        CompositeID = $"{sid}:{uid}";
        UserSnowflake = uid;
        ServerSnowflake = sid;
    }

    public ServerUser()
    {
        
    }
    [Key]
    public string CompositeID { get; set; }
    public ulong UserSnowflake { get; set; }
    public ulong ServerSnowflake { get; set; }
    public int Earnings { get; set; } = 0;
    public int Balance { get; set; } = 0;
    public float Multiplier { get; set; } = 1.0f;
    public string Nicklock { get; set; } = "";
    public string? PrevNick { get; set; } = "";
    public DateTime? NicklockUntil { get; set; }
    public bool Authoritative { get; set; } = false;
    public bool Censored { get; set; } = false;
    public bool Immune { get; set; } = false;
    public bool Francophone { get; set; } = false;
    public string RoleBackup { get; set; } = "";
    public DateTime? IdiotedUntil { get; set; }
    public DateTime? DeflectorExpiry { get; set; }
    public Attitude SentinelAttitude { get; set; } = Attitude.Neutral;

    public bool ValidDeflector()
    {
        if (DeflectorExpiry == null) return false;
        return DeflectorExpiry.Value > DateTime.Now;
    }

    public enum Attitude
    {
        Neutral,
        Friendly,
        Belligerent 
    }
    
}



public class ServerWarns
{
    [Key]
    public uint warnid { get; set; }
    public ulong serverid { get; set; }
    public ulong warner { get; set; }
    public ulong warned { get; set; }
    public DateTime warnTime { get; set; }
    public string warnReason { get; set; }
}

public class Reaction
{
    [Key] 
    public int ReactionId { get; set; }
    public ulong MessageId  { get; set; }
    public ulong ReactorId  { get; set; }
    public ulong ReacteeId  { get; set; }
    public ulong? ServerId { get; set; }
    public string ReactName { get; set; }

    public Reaction () {}

    public Reaction(SocketReaction r, IMessage msg)
    {
        MessageId = r.MessageId;
        ReactorId = r.UserId;
        ReacteeId = msg.Author.Id;
        ReactName = r.Emote.Name;
        if(r.Channel is SocketGuildChannel sgc) ServerId = sgc.Guild.Id;
    }
    
}
public class ServerConfig
{
    public ServerConfig(ulong id)
    {
        DiscordID = id;
    }
    
    public ServerConfig() {}
    
    [Key]
    public ulong DiscordID { get; set; }
    public ulong? FlagChannel { get; set; }
    public ulong? ModRole { get; set; }
    public int MuteCost { get; set; } = 250;
    public int NickCost { get; set; } = 100;
    public int DeflectorCost { get; set; } = 500;
    public int Cost1984 { get; set; } = 250;
    public int CostDe1984 { get; set; } = 100;
    public int CostWarn { get; set; } = 50;
    public int FrenchCost { get; set; } = 250;
    public float RewardChance { get; set; } = 0.10f;
    public int RewardSize { get; set; } = 5;
    public bool FunnyCommands { get; set; } = false;
    public List<CensorEntry> Censor { get; set; } = new List<CensorEntry>();
    public List<AutoResponse> AutoResponses { get; set; } = new List<AutoResponse>();
    public List<QuoteEntry> Quotes { get; set; } = new List<QuoteEntry>();
    public ulong? IdiotRole { get; set; }
    public TimeSpan DefaultSentence { get; set; } = TimeSpan.FromDays(90);
    
    public string GetRandomQuote()
    {
        if (Quotes.Count == 0) return "brain empty 😭. fill me with nonsense.";
        var x = Quotes[RandomNumberGenerator.GetInt32(0, Quotes.Count)];
        return x.Text;
    }
}

public class AutoResponse
{
    [JsonIgnore]
    [Key] 
    public int ResponseId { get; set; }
    public string Trigger { get; set; } = "";
    public ulong? TargetUser { get; set; }
    public ulong? TargetChannel { get; set; }
    public bool Wildcard { get; set; } = false;
    public string? ResponseText { get; set; }
    public string? ResponseEmote { get; set; }
    public int Chance { get; set; } = 100;

    public bool RateLimit { get; set; } = false;
    public DateTime? LastTrigger { get; set; }
    public TimeSpan? ReloadTime { get; set; }

    public async Task<AutoResponse?> Triggered(IUserMessage msg)
    {
        if (TargetUser != null) if (msg.Author.Id != TargetUser) return null;
        if (TargetChannel != null) if (msg.Channel.Id != TargetChannel) return null;
        if (RateLimit && LastTrigger != null && ReloadTime != null && LastTrigger + ReloadTime > DateTime.Now) return null;
        if (Match(msg.Content) && Chance >= RandomNumberGenerator.GetInt32(0, 101)) return this;
        return null;
    }
    
    public bool Match(string input)
    {
        input = input.ToUpper();
        bool match = false;
        if (Wildcard) match = input.Contains(Trigger);
        else
        {
            Regex rx = new(@$"\b{Trigger}\b");
            match = rx.IsMatch(input);
        }
        return match;
    }

    public async Task Execute(IUserMessage msg, ServerConfig srv)
    {
        LastTrigger = DateTime.Now;
        if (ResponseText != null)
        {
            if (ResponseText == "RANDOMQUOTE")
            {
                await msg.ReplyAsync(srv.GetRandomQuote());
            }
            else
            {
                await msg.ReplyAsync(ResponseText);
            }
        }
        if (ResponseEmote != null)
        {
            if (Emoji.TryParse(ResponseEmote, out var emoji))
            {
                await msg.AddReactionAsync(emoji);
            }
            else if (Emote.TryParse(ResponseEmote, out var emote))
            {
                await msg.AddReactionAsync(emote);
            }
                    
        }
    }
    
}

public class CensorEntry
{
    [JsonIgnore]
    public int Id { get; set; }
    public string Phrase { get; set; } = "";
    public bool Requirement { get; set; } = false;
    public bool Wildcard { get; set; } = true;

    public bool IsCensored(string input)
    {
        bool match = false;
        if (Wildcard) match = input.Contains(Phrase);
        else
        {
            Regex rx = new(@$"\b{Phrase}\b");
            match = rx.IsMatch(input);
        }
        if (Requirement) return !match;
        return match;
    }
    
}

public class OCREntry
{
    public int Id { get; set; }
    public ulong Server { get; set; }
    public ulong Channel { get; set; }
    public ulong Message { get; set; }
    public string ImageURL { get; set; }
    public byte[] ImageHash { get; set; }
    public string Text { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public ulong ServerID { get; set; }
    public ulong RecipientID { get; set; }
    public ulong SenderID { get; set; }
    public int Amount { get; set; }
    public TxnType Reason { get; set; }
        
    public enum TxnType
    {
        Print,
        RewardReaction,
        RewardMessage,
        Transfer,
        Seizure,
        Refund,
        Purchase,
        Theft,
        Tax,
        StartingBalance
    }

    public enum TxnStatus
    {
        Success,
        InsufficientFunds,
        Invalid,
        Failed
    }
}

public class QuoteEntry
{
    [JsonIgnore]
    [Key]
    public int Id { get; set; }
    public ulong ServerId { get; set; }
    public string Text { get; set; } = "";
}

public class Vote
{
    public int Id { get; set; }
    public ulong Poll { get; set; }
    public ulong User { get; set; }
}