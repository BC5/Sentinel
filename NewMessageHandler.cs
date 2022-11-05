using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Sentinel.Archivist;

namespace Sentinel;

public class NewMessageHandler
{
    private DiscordSocketClient _discord;
    private Sentinel _core;
    private OCRManager _ocr;
    private Sentinel.Regexes _regexes;
    private Detention _detention;
    private Config _config;
    private Random _random;

    public NewMessageHandler(DiscordSocketClient discord, Sentinel core, OCRManager ocr, Sentinel.Regexes regex,
        Detention detention, Config conf, Random rand)
    {
        _discord = discord;
        _core = core;
        _ocr = ocr;
        _regexes = regex;
        _detention = detention;
        _config = conf;
        _random = rand;
    }
    
    public async Task NewMessage(SocketMessage msg)
    {
        //Harass Nelson
        if (msg.Content.Contains("@Nelson") && !msg.Author.IsBot)
        {
            var tfchannel = (IMessageChannel) (await _discord.GetChannelAsync(1005607649318023298));
            await tfchannel.SendMessageAsync($"A Message From {msg.Author.Mention} For <@862107857641013248>!\n{msg.Content}");
        }
        
        var data = _core.GetDbContext();
        
        if(!(msg.Channel is SocketGuildChannel)) return;
        SocketGuildChannel channel = (SocketGuildChannel) msg.Channel;
        ServerUser user = await data.GetServerUser(msg.Author.Id, channel.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(channel.Guild.Id);

        SocketGuildUser? sgu = null;
        if (msg.Author is SocketGuildUser sgutemp) sgu = sgutemp;
        
        //OCR
        OCR(msg);

        //New Idiot & Add x Days check
        bool suppressquote = await NewIdiot(sgu,msg,srv,channel,data);
        
        //Trigger random reply when pinged
        if (!suppressquote && sgu != null) await RandomQuote(msg, sgu, user, srv, data);

        //Do autoresponses
        await AutoResponses(msg);

        //AntiChristmas
        await AntiChristmas(msg);
        
        //No bot messages past this point
        if(msg.Author.IsBot) return;
        
        //Apply censor
        await Censor(msg, user, srv);

        await data.SaveChangesAsync();
    }

    private void OCR(IMessage msg)
    {
        //OCR
        _ocr.EnqueueMessage(msg);
    }

    public async Task AntiChristmas(SocketMessage msg)
    {
        //Anti Christmas Aktion
        if (msg.Embeds != null && msg.Embeds.Count > 0)
        {
            if(msg.Embeds.First().Title == null) return;
            string title = msg.Embeds.First().Title.ToUpper();
            if (title.Contains("CHRISTMAS") || title.Contains("XMAS"))
            {
                await ((SocketGuildUser) msg.Author).SetTimeOutAsync(TimeSpan.FromMinutes(1));
                await msg.Channel.SendMessageAsync("<:garf1984:1019347975648063589>");
                await msg.DeleteAsync();
                return;
            }
        }
    }

    private async Task<bool> NewIdiot(SocketGuildUser? sgu, IMessage msg, ServerConfig srv, SocketGuildChannel channel, Data data)
    {
        if (sgu != null && sgu.GuildPermissions.ModerateMembers && msg.Reference != null && msg.Reference.MessageId.IsSpecified)
        {
            //Check for new @idiot and add <x> days
            Match m1 = _regexes.NewIdiot.Match(msg.Content);
            Match m2 = _regexes.AddDays.Match(msg.Content);
            if (m1.Success || m2.Success)
            {
                Match m = m1.Success ? m1 : m2;
                
                var msgref = await msg.Channel.GetMessageAsync(msg.Reference.MessageId.Value);
                bool cont = true;

                if (m1.Success && m.Groups[1].Value != srv.IdiotRole.ToString()) cont = false;
                if (!m1.Success && m.Groups[1].Value != _discord.CurrentUser.Id.ToString()) cont = false;
                if (cont)
                {
                    if (msgref != null)
                    {
                        try
                        {
                            TimeSpan sentence = srv.DefaultSentence;
                            if (!m1.Success && int.TryParse(m.Groups[2].Value, out int days))
                            {
                                sentence = TimeSpan.FromDays(days);
                            }
                            await _detention.ModifySentence(msgref, channel, sentence, data);
                            return true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }
        return false;
    }

    private async Task RandomQuote(SocketMessage msg, SocketGuildUser sgu, ServerUser user, ServerConfig srv, Data data)
    {
        foreach (var mention in msg.MentionedUsers)
        {
            if (mention.Id == _discord.CurrentUser.Id)
            {
                if (msg is SocketUserMessage msg2)
                {
                    if (msg2.ReferencedMessage != null &&
                        msg2.ReferencedMessage.Author.Id == _discord.CurrentUser.Id) break;
                    var quote = _config.GetQuote();

                    quote = await _config.QuoteProcess(quote, sgu, user, _detention, srv, data);
                    await msg2.ReplyAsync(quote);
                }
            }
        }
    }

    private async Task AutoResponses(SocketMessage msg)
    {
        List<Task<bool>> checks = new();
        if (msg is IUserMessage msg3)
        {
            string mcont = msg.Content.ToUpper();
            foreach (var ar in _config.AutoResponses)
            {
                checks.Add(ar.Check(msg3,mcont,_config));
            }
        }
        await Task.WhenAll(checks);
    }

    private async Task Censor(SocketMessage msg, ServerUser user, ServerConfig srv)
    {
        if (user.Censored && srv.FunnyCommands)
        {
            if (CensorCheck(msg, srv))
            {
                await msg.DeleteAsync();
                var dms = await msg.Author.CreateDMChannelAsync();
                await dms.SendMessageAsync("Oopsie! You fell afoul of the censor!\n- Use /censor check in the server to see the rules\n- Or free yourself for £100 with /censor free");
            }
        }
    }

    private bool CensorCheck(SocketMessage msg, ServerConfig srv)
    {
        string msgcontent = msg.Content.ToUpper();
        foreach (CensorEntry censor in srv.Censor) if (censor.IsCensored(msgcontent)) return true;
        return false;
    }
    
}