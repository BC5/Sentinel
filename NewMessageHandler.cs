﻿using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Sentinel.Archivist;
using Sentinel.Logging;

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
    private MessageManagement _msgMgr;
    private SentinelLogging _log;

    public NewMessageHandler(DiscordSocketClient discord, Sentinel core, OCRManager ocr, Sentinel.Regexes regex,
        Detention detention, Config conf, Random rand, TextCat textcat, MessageManagement msgMgr, SentinelLogging log)
    {
        _discord = discord;
        _core = core;
        _ocr = ocr;
        _regexes = regex;
        _detention = detention;
        _config = conf;
        _random = rand;
        _textcat = textcat;
        _msgMgr = msgMgr;
        _log = log;
    }
    
    public async Task NewMessage(SocketMessage msg)
    {
        using (var data = _core.GetDb())
        {
            //Start voicenote download
            Task voiceNote = StashVoiceNote(msg);
            Task msgMgr = _msgMgr.MessageLog(msg);

            if (!(msg.Channel is SocketGuildChannel)) return;
            SocketGuildChannel channel = (SocketGuildChannel) msg.Channel;
            ServerUser user = await data.GetServerUser(msg.Author.Id, channel.Guild.Id);
            ServerConfig srv = await data.GetServerConfig(channel.Guild.Id);
            SocketGuildUser? sgu = null;
            if (msg.Author is SocketGuildUser sgutemp) sgu = sgutemp;

            //OCR
            OCR(msg);

            //New Idiot & Add x Days check
            bool suppressquote = await NewIdiot(sgu, msg, srv, channel, data);
            
            //Trigger random reply when pinged
            if (!suppressquote && sgu != null) await RandomQuote(msg, sgu, user, srv, data);
            
            //Do autoresponses
            await AutoResponses(msg, srv);
            
            //Social Credit Actions
            await SocialCreditActions(msg, SocialCreditCommands.GetClass(user.SocialCredit));

            //Check for "JUVE"
            if (user.Juvecheck) await JuveCheck(msg);
            
            //AntiChristmas
            //await AntiChristmas(msg);

            //No bot messages
            if (!msg.Author.IsBot)
            {
                //Apply censor
                await Censor(msg, user, srv);

                //Apply Frenchification
                await Francais(msg, user, srv, _textcat);
            }
            
            //Await pending tasks
            await voiceNote;
            await msgMgr;
            //Save changes
            await data.SaveChangesAsync();
        }
    }

    private async Task JuveCheck(SocketMessage msg)
    {
        bool failed = false;
        foreach (var embed in msg.Embeds)
        {
            if (embed.Type == EmbedType.Video)
            {
                failed = true;
            }
        }

        foreach (var attachment in msg.Attachments)
        {
            if (attachment.ContentType.ToLower().Contains("video"))
            {
                failed = true;
            }
        }

        if (msg.Content.ToLower().Contains("http") && (msg.Content.ToLower().Contains(".mp4") || msg.Content.ToLower().Contains(".webm")))
        {
            failed = true;
        }

        if (failed)
        {
            var message = (SocketUserMessage) msg;
            await message.ReplyAsync("Oops. I can't check that file for the text \"JUVE\". I can't let you post things without juve in them now can I?");
            await message.DeleteAsync();
        }
        
    }

    private async Task StashVoiceNote(SocketMessage msg)
    {
        if (msg.Attachments != null && msg.Attachments.Count > 0)
        {
            var attach = msg.Attachments.First();
            if (attach.Filename.Contains(".ogg"))
            {
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(attach.ProxyUrl,$"{_config.DataDirectory}/stash/{msg.Author.Username}-{msg.Id}.ogg");
                }
            }
        }
    }

    private void OCR(IMessage msg)
    {
        //OCR
        _ocr.EnqueueMessage(msg);
    }

    public async Task SocialCreditActions(SocketMessage msg, SocialCreditCommands.CreditClass sc)
    {
        if (sc < SocialCreditCommands.CreditClass.Nuisance && msg is IUserMessage um)
        {
            if (_random.Next(50) == 20)
            {
                await um.ReplyAsync(_config.GetBelligerentResponse());
            }
            else if(sc < SocialCreditCommands.CreditClass.Menace && _random.Next(75) == 30)
            {
                await um.ReplyAsync("deleting this. kys.");
                await um.DeleteAsync();
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

    public static async Task Francais(SocketMessage msg, ServerUser user, ServerConfig srv, TextCat textcat)
    {
        if(msg.Channel.Id != srv.FrenchChannel && !user.Francophone) return;
        if(msg.Content == "") return;
        
        if (!textcat.IsFrench(msg.Content) && msg is SocketUserMessage umsg)
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
                    if (msg2.ReferencedMessage != null)
                    {
                        if (msg2.ReferencedMessage.Author.Id == _discord.CurrentUser.Id && msg2.Author.Id == _discord.CurrentUser.Id)
                        {
                            break;
                        }
                    }
                    var quote = srv.GetRandomQuote();

                    quote = await _config.QuoteProcess(quote, sgu, user, _detention, srv, data);
                    await msg2.ReplyAsync(quote);
                }
            }
        }
    }

    private async Task AutoResponses(SocketMessage msg, ServerConfig srv)
    {
        if(msg.Author.IsBot) return;

        List<Task<AutoResponse?>> checks = new(); 

        if (msg is IUserMessage msg3)
        {
            foreach (var ar in srv.AutoResponses)
            {
                checks.Add(ar.Triggered(msg3));
            }
            await Task.WhenAll(checks);
            int actioned = 0;
            foreach (var task in checks)
            {
                if (task.Result != null)
                {
                    actioned++;
                    await task.Result.Execute(msg3,srv);
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
                await ((IGuildUser) msg.Author).SetTimeOutAsync(TimeSpan.FromSeconds(30));
                var dms = await msg.Author.CreateDMChannelAsync();
                await dms.SendMessageAsync($"Oopsie! You fell afoul of the censor!\n- Use /censor check in the server to see the rules\n- Or free yourself for £{srv.CostDe1984:n0} with /censor free");
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