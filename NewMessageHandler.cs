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
    private TextCat _textcat;

    public NewMessageHandler(DiscordSocketClient discord, Sentinel core, OCRManager ocr, Sentinel.Regexes regex,
        Detention detention, Config conf, Random rand, TextCat textcat)
    {
        _discord = discord;
        _core = core;
        _ocr = ocr;
        _regexes = regex;
        _detention = detention;
        _config = conf;
        _random = rand;
        _textcat = textcat;
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
        
        //Attitude
        await Attitude(msg, user.SentinelAttitude);

        //AntiChristmas
        //await AntiChristmas(msg);
        
        //No bot messages past this point
        if (msg.Author.IsBot)
        {
            await data.SaveChangesAsync();
            return;
        }
        
        //Apply censor
        await Censor(msg, user, srv);
        
        //Apply Frenchification
        await Francais(msg, user);
        
        await data.SaveChangesAsync();
    }

    private void OCR(IMessage msg)
    {
        //OCR
        _ocr.EnqueueMessage(msg);
    }

    public async Task Attitude(SocketMessage msg, ServerUser.Attitude attitude)
    {
        if (attitude == ServerUser.Attitude.Belligerent)
        {
            if (_random.Next(50) == 20 && msg is IUserMessage um)
            {
                await um.ReplyAsync(_config.GetBelligerentResponse());
            }
        }
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

    private async Task Francais(SocketMessage msg, ServerUser user)
    {
        if(msg.Channel.Id != 1021889219209211904 && !user.Francophone) return;

        if(msg.Content == "") return;
        
        if (!_textcat.IsFrench(msg.Content) && msg is SocketUserMessage umsg)
        {
            var msgs = msg.Channel.GetCachedMessages(msg, Direction.Before, 1);
            bool reply = true;
            if (msgs != null && msgs.Count > 0)
            {
                var previous = msgs.First();
                if (previous != null && previous.Content == "Parlez Français. C’est obligatoire") reply = false;
            }
            if(reply) await umsg.ReplyAsync("Parlez Français. C’est obligatoire");
            await msg.DeleteAsync();
        }
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
        if(msg.Author.IsBot) return;

        List<Task<Config.AutoResponse?>> checks = new();
        if (msg is IUserMessage msg3)
        {
            string mcont = msg.Content.ToUpper();
            foreach (var ar in _config.AutoResponses)
            {
                checks.Add(ar.Check(msg3));
            }
            await Task.WhenAll(checks);
            int actioned = 0;
            foreach (var task in checks)
            {
                if (task.Result != null)
                {
                    actioned++;
                    await task.Result.Action(msg3,_config);
                    if(actioned >= 2) return;
                }
            }
        }
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