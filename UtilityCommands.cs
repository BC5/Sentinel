using System.Security.Cryptography;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sentinel.Archivist;
using Sentinel.Logging;
using Sentinel.Procedures;
using ContextType = Discord.Commands.ContextType;

namespace Sentinel;

public class UtilityCommands : InteractionModuleBase
{
    private Sentinel _core;
    private OCRManager _ocr;
    private ProcedureScheduler _procSched;
    private Data _data;
    private SentinelLogging _log;

    public UtilityCommands(Sentinel sentinel, OCRManager ocr, ProcedureScheduler procSched, Data data, SentinelLogging log)
    {
        _core = sentinel;
        _ocr = ocr;
        _procSched = procSched;
        _data = data;
        _log = log;
    }

    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "webhooksend", description: "Push to a webhook")]
    public async Task WebhookSend(string url, string message)
    {
        if(Context.User.Id != 241325827810131978) return;
        var whc = new DiscordWebhookClient(url);
        await whc.SendMessageAsync(message);
        await RespondAsync("Done", ephemeral: true);
    }
    
    [Discord.Interactions.RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("logchannel","Channel where logs go")]
    public async Task LogChannel([ChannelTypes(ChannelType.Text)] IGuildChannel channel)
    {
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);

        if (srv.LogChannel == channel.Id)
        {
            srv.LogChannel = null;
            await _data.SaveChangesAsync();
            await RespondAsync($"Log Channel is now disabled");
        }
        else
        {
            srv.LogChannel = channel.Id;
            await _data.SaveChangesAsync();
            await RespondAsync($"Log Channel is now <#{channel.Id}>");
        }
    }
    
    [Discord.Interactions.RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("reactboard","Setup a react board")]
    public async Task ReactBoard([ChannelTypes(ChannelType.Text)] IGuildChannel channel, string react, int threshold, bool remove = false)
    {
        ServerConfig srv = await _data.GetServerConfig(Context.Guild.Id);
        if (!remove)
        {
            ReactBoardConfig rbc = new ReactBoardConfig()
            {
                ChannelId = channel.Id,
                Reaction = react,
                Threshold = (uint) threshold
            };
            srv.ReactBoards.Add(rbc);
            await _data.SaveChangesAsync();
            await RespondAsync($"<#{channel.Id}> will be a {react}board");
        }
        else
        {
            var rb = srv.ReactBoards.SingleOrDefault(x => x.ChannelId == channel.Id && x.Reaction == react);
            if (rb == null)
            {
                await RespondAsync("Couldn't find a board to delete with those parameters");
            }
            else
            {
                srv.ReactBoards.Remove(rb);
                await _data.SaveChangesAsync();
                await RespondAsync($"<#{channel.Id}> will no longer be a {react}board");
            }
        }
    }

    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "logs", description: "Get some log entries")]
    public async Task FetchLogs(int fetch = 10, LogType level = LogType.Info, bool ephemeral = false, TimestampType timestamps = TimestampType.DateAndTime)
    {
        LogType levelFlag = SentinelLogging.GetLogLevelFlag(level);
        await _log.Info("/logs",$"{Context.User.Username} accessed logs ({fetch} entries, {level})");
        string log = "Logs:\n";

        if (_log.LogEntries == null)
        {
            await RespondAsync("Logs are not being cached.",ephemeral: ephemeral);
            return;
        }
        
        int start = Math.Max(0, _log.LogEntries.Count-fetch);
        int j = 0;

        char ts = 'R';
        switch (timestamps)
        {
            case TimestampType.Relative:
                ts = 'R';
                break;
            case TimestampType.Time:
                ts = 'T';
                break;
            case TimestampType.DateAndTime:
                ts = 'f';
                break;
        }
        
        for (int i = start; i < _log.LogEntries.Count; i++)
        {
            var entry = _log.LogEntries[i];
            if ((levelFlag & entry.Level) == entry.Level)
            {
                log = log + $"<t:{entry.Timestamp.ToUnixTimeSeconds()}:{ts}> `[{entry.Source}] {entry.Message}\n`";
                j++;
            }
        }

        log = log + $"**{j} entries returned out of {_log.LogEntries.Count}**";
        
        await RespondAsync(log,ephemeral: ephemeral);
    }

    public enum TimestampType
    {
        Relative,
        Time,
        DateAndTime
    }

    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "execute", description: "Execute XML-Defined Procedure")]
    public async Task ExecuteProcedure(string proc, string? schedule = null)
    {
        if (schedule != null)
        {
            DateTime dt;
            bool success = DateTime.TryParse(schedule, out dt);
            if (!success)
            {
                await RespondAsync($"I don't know what time \"{schedule}\" is");
                return;
            }
            long ts = ((DateTimeOffset) dt).ToUnixTimeSeconds();
            await RespondAsync($"Ok. It should trigger <t:{ts}:R> (<t:{ts}:f>)");
            await _procSched.Add(new ProcedureScheduler.ScheduledProcedure()
            {
                ProcedureName = proc.ToUpper(),
                ProcedureTrigger = dt
            });
        }

        proc = proc.ToUpper();
        await DeferAsync();
        string procpath = $@"{_core.GetConfig().DataDirectory}/procedures/{proc}.xml";
        if (!File.Exists(procpath))
        {
            await FollowupAsync($"Could not find `PROCEDURE-{proc}`");
        }
        string xmlstr = await File.ReadAllTextAsync(procpath);
        try
        {
            SentinelProcedure procedure = SentinelProcedure.Deserialise(xmlstr,_core);
            ActionStatus status = await procedure.Execute();
            await FollowupAsync($"`PROCEDURE-{proc}` EXECUTION {status}");
        }
        catch (Exception e)
        {
            await FollowupAsync($"`PROCEDURE-{proc}` Errored. See console.");
            Console.WriteLine(e);
            return;
        }
    }
    
    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "status", description: "Change bot status")]
    public async Task Status(string message, ActivityType type = ActivityType.Playing)
    {
        await _core.GetClient().SetGameAsync(message, type: type);
        await RespondAsync("Done",ephemeral:true);
    }
    
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [Discord.Interactions.RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
    [SlashCommand("botnick", "Change the bot's nickname")]
    public async Task BotNick([MaxLength(32)] string nickname)
    {
        try
        {
            var u = await Context.Guild.GetCurrentUserAsync();
            await u.ModifyAsync(x => x.Nickname = nickname);
            await RespondAsync($"Changed my nickname to {nickname}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await RespondAsync("Ran into a problem. Maybe you put some stupid special character in there or I don't have permission?");
        }
        
    }

    [SlashCommand(name: "warnings", description: "See a user's warnings")]
    public async Task Warnings(IGuildUser user)
    {
        List<ServerWarns> warns = await _data.Warns.Where(w => w.serverid == Context.Guild.Id && w.warned == user.Id).ToListAsync();
        if (warns.Count == 0)
        {
            await RespondAsync($"No warns found for {user.Mention}");
            return;
        }
        var embed = new EmbedBuilder();
        embed.WithTitle($"Warnings for {user.Username}");
        embed.WithColor(Color.Red);
        string desc = "";
        foreach (var warn in warns)
        {
            string warntext = warn.warnReason;
            if (warntext.Length > 250)
            {
                warntext = warntext.Substring(0, 240);
            }
            
            desc = desc + $"<@{warn.warner}> : {warntext}\n";
        }
        embed.WithDescription(desc);
        embed.WithFooter($"{warns.Count} warnings");
        try
        {
            await RespondAsync(embed: embed.Build());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    [MessageCommand("OCR")]
    public async Task OCRCheck(IMessage msg)
    {
        if (msg.Attachments.Count == 0 && msg.Embeds.Count == 0)
        {
            await RespondAsync("Don't think there's any image there 😬", ephemeral: true);
            return;
        }
        var embed = new EmbedBuilder();
        embed.WithTitle("OCR");
        var results = await _data.OcrEntries.Where(m => m.Message == msg.Id).ToListAsync();
        string txt = "";
        foreach (var r in results)
        {
            txt = txt + r.Text;
        }

        embed.WithDescription(txt);
        if (results.Count == 0)
        {
            await RespondAsync("I haven't got this indexed. Maybe there's no text or it's in the queue?",
                ephemeral: true);
        }
        else
        {
            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }
    }

    [MessageCommand("FactCheck")]
    public async Task FactCheck(IMessage msg)
    {
        var srvtsk = _data.GetServerConfig(Context.Guild.Id);
        var usrtsk = _data.GetServerUser((SocketGuildUser) Context.User);
        var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(msg.Content.ToLower()));
        int seed = BitConverter.ToInt32(hash);
        var srv = await srvtsk;
        var usr = await usrtsk;

        if (usr.Balance < srv.FactcheckCost)
        {
            await RespondAsync($"Brokie. Can't even afford a factcheck lmao (£{srv.FactcheckCost:n0})");
            return;
        }

        var txn = await _data.Transact(usr, null, srv.FactcheckCost, Transaction.TxnType.Purchase);

        if (msg is IUserMessage msg2 && txn == Transaction.TxnStatus.Success)
        {
            await RespondAsync("✅",ephemeral:true);

            if (msg.Channel.Id == srv.FrenchChannel)
            {
                await msg2.ReplyAsync("**VÉRIFIÉ PAR DE VRAIS PATRIOTES FRANÇAIS:** JE ME RENDS");
                return;
            }
            
            Config cfg = _core.GetConfig();
            Config.FactCheck fc = cfg.GetFactcheck(seed);
            
            //If it's me just keep rerolling until you get true
            if (msg.Author.Id == 241325827810131978)
            {
                while (fc.Type != Config.FactCheck.CheckType.TRUE)
                {
                    fc = cfg.GetFactcheck();
                }
            }

            string text = fc.Text;
            
            if (text.Contains("<TRIGGERUSER>"))
            {
                text = text.Replace("<TRIGGERUSER>", Context.User.Mention);
            }
            
            await msg2.ReplyAsync(text);
            return;
        }

        await RespondAsync("Something went wrong.", ephemeral: true);
    }

    /*
    [SlashCommand(name: "topmessages", description: "See top 5 messages with given emote reacts")]
    public async Task TopMessages()
    {
        await DeferAsync();
        await _data.TopByReact("thake", 1019326226713817138);

    }
    */

    [SlashCommand(name: "ocrsearch", description: "Search posted images' text")]
    public async Task OCRQuery(string query)
    {
        try
        {
            List<OCREntry> results = await _ocr.QueryIndex(query, Context.Channel.Id);
            await DeferAsync(ephemeral: true);
            if (results.Count == 0)
            {
                await Context.Interaction.FollowupAsync("No results found");
            }
            else
            {
                string resultstr, descr;
                if (results.Count == 1) resultstr = "1 Result Found";
                else resultstr = $"{results.Count} Results Found";
                if (results.Count >= 5) descr = "Showing 5, sorted by relevance";
                else descr = $"Showing {results.Count}, sorted by relevance";

                var embed = new EmbedBuilder()
                {
                    Title = resultstr,
                    Description = descr,
                    Color = Color.Green
                };

                for (int i = 0; i < results.Count && i < 5; i++)
                {
                    string url = $"https://discord.com/channels/{results[i].Server}/{results[i].Channel}/{results[i].Message}";
                    string txt = results[i].Text;
                    if (txt.Length > 62)
                    {
                        txt = txt.Substring(0, 60) + "...";
                    }
                    txt = txt.ReplaceLineEndings(" ");
                    embed = embed.AddField($"#{(i + 1)}", value: $"{txt} - [\\[link\\]]({url})");
                }

                await Context.Interaction.FollowupAsync(embed: embed.Build());
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Discord.Interactions.RequireOwner]
    [SlashCommand(name: "ocrindex", description: "Search posted images' text")]
    public async Task OcrIndex([ChannelTypes(ChannelType.Text)] IGuildChannel channel)
    {
        if (_ocr.BeginIndexing((IMessageChannel) channel, Context.User))
        {
            await RespondAsync($"Started indexing <#{channel.Id}>");
        }
        else
        {
            await RespondAsync("Already indexing something. Try again later.");
        }
    }
    
    [SlashCommand(name: "addquote", description: "Add a quote to the repository")]
    public async Task AddQuote(string quote)
    {
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        var usr = await _data.GetServerUser(Context.User.Id, srv.DiscordID);

        if (!usr.Authoritative)
        {
            await RespondAsync("minion ehh no gif");
            return;
        }
        
        if (srv.Quotes.Any(x => x.Text == quote))
        {
            await RespondAsync("Already added",ephemeral:true);
            return;
        }
        
        srv.Quotes.Add(new QuoteEntry() {ServerId = Context.Guild.Id, Text = quote});
        await _data.SaveChangesAsync();
        await RespondAsync($"Added \"{quote}\".\nI've got {srv.Quotes.Count:n0} quotes now (😬)");
    }
}