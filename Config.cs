using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using ImageMagick;
using Sentinel.Procedures;

namespace Sentinel;

public class Config
{
    public string DiscordToken { get; set; } = "N/A";
    public string TwitterAPIKey { get; set; } = "";
    public string TwitterAPISecret { get; set; } = "";
    public string TwitterAPIToken { get; set; } = "";
    
    public ulong[] AdminUsers { get; set; } = Array.Empty<ulong>();
    public bool GlobalMode { get; set; } = false;
    public ulong[] Servers { get; set; } = Array.Empty<ulong>();
    public string[] Statuses { get; set; } = Array.Empty<string>();
    public List<FactCheck> FactChecks { get; set; } = new();
    public List<string> Quotes { get; set; } = new();
    public List<string> BelligerentResponses { get; set; } = new();
    public string DataDirectory { get; set; } = @".\data";
    public List<AutoResponse> AutoResponses { get; set; } = new();
    public List<ProcedureScheduler.ScheduledProcedure> ProcedureSchedule { get; set; } = new();


    public string GetStatus()
    {
        return Statuses[RandomNumberGenerator.GetInt32(0, Statuses.Length)];
    }
    
    public string GetQuote()
    {
        return Quotes[RandomNumberGenerator.GetInt32(0, Quotes.Count)];
    }
    
    public string GetBelligerentResponse()
    {
        return BelligerentResponses[RandomNumberGenerator.GetInt32(0, BelligerentResponses.Count)];
    }

    public async Task<string> QuoteProcess(string quote, SocketGuildUser sender, ServerUser susender, Detention detention, ServerConfig sconf, Data data)
    {
        if (quote.Contains("<PINGER>"))
        {
            quote = quote.Replace("<PINGER>", sender.Mention);
        }
                    
        if (quote.Contains("<MUTEUSER>"))
        {
            quote = quote.Replace("<MUTEUSER>", "");
            await sender.SetTimeOutAsync(TimeSpan.FromMinutes(5));
        }
        
        if (quote.Contains("<IDIOTUSER>"))
        {
            quote = quote.Replace("<IDIOTUSER>", "");
            await detention.ModifySentence(sender, susender, sconf, TimeSpan.FromMinutes(30));
            await data.SaveChangesAsync();
        }

        if (quote.Contains("<REWARDUSER>"))
        {
            quote = quote.Replace("<REWARDUSER>", "");
            await data.Transact(null, susender, 100, Transaction.TxnType.RewardMessage, allowDebt: true);
        }
        
        //mango whinged at me >:(
        if (quote == "If I am not black how come I just DMed you a racial slur")
        {
            var dms = await sender.CreateDMChannelAsync();
            await dms.SendMessageAsync("https://en.wikipedia.org/wiki/List_of_ethnic_slurs#N");
        }

        return quote;
    }

    public FactCheck GetFactcheck()
    {
        return FactChecks[RandomNumberGenerator.GetInt32(0, FactChecks.Count)];
    }
    
    public FactCheck GetFactcheck(int seed)
    {
        return FactChecks[Math.Abs(seed % FactChecks.Count)];
    }


    public class FactCheck
    {
        public string Text { get; set; } = "";
        public CheckType Type { get; set; } = CheckType.TRUE;

        public FactCheck(){}

        public FactCheck(string text, CheckType type)
        {
            this.Type = type;
            this.Text = text;
        }
        
        public enum CheckType
        {
            TRUE,
            FALSE,
            UNCERTAIN
        }
        
    }
    
    public class AutoResponse
    {
        public string Trigger { get; set; }
        public ulong? UserTarget { get; set; }
        public bool Wildcard { get; set; } = false;
        public string? TextResponse { get; set; }
        public string? EmoteResponse { get; set; }

        public async Task<AutoResponse?> Check(IUserMessage msg)
        {
            if (UserTarget != null)
            {
                if (UserTarget.Value != msg.Author.Id) return null;
            }
            
            if (Match(msg.Content))
            {
                return this;
            }
            return null;
        }

        public async Task Action(IUserMessage msg, Config cfg)
        {
            if (TextResponse != null)
            {
                if (TextResponse == "RANDOMQUOTE")
                {
                    await msg.ReplyAsync(cfg.GetQuote());
                }
                else
                {
                    await msg.ReplyAsync(TextResponse);
                }
            }
            if (EmoteResponse != null)
            {
                if (Emoji.TryParse(EmoteResponse, out var emoji))
                {
                    await msg.AddReactionAsync(emoji);
                }
                else if (Emote.TryParse(EmoteResponse, out var emote))
                {
                    await msg.AddReactionAsync(emote);
                }
                    
            }
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
        
    }
    
}