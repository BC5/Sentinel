using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Sentinel.Bot.Archivist;
using Sentinel.Bot.ImageProcessing.Operations;
using Sentinel.Bot.Procedures;
using Sentinel.Bot.ImageProcessing;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PreconditionResult = Discord.Interactions.PreconditionResult;

namespace Sentinel.Bot;

public class SentinelBot
{

    private DiscordSocketClient _discord;
    private InteractionService _interactions;
    private ServiceProvider _services;
    private Config _config;
    private AutoMod _automod;
    private AssetManager _assets;
    private OCRManager _ocr;
    private Detention _detention;
    private ProcedureScheduler _procScheduler;
    private Random _random;
    private Regexes _regexes;
    private NewMessageHandler _newMessageHandler;
    private TwitterManager _twitter;
    private MassDeleter _deleter;
    private TextCat _textcat;

    private SentinelCore _core;

    private ulong _ticks = 0;

    private List<ISentinelModule> _modules;
    public SentinelEvents Events { get; set; }
    private string _configfile = "";

    public static string SentinelVersion = "1.2.0";

    public SentinelDatabase GetDbContext()
    {
        return _core.GetDbContext();
    }

    public SentinelBot(Config conf, string cfgloc, SentinelCore core)
    {
        _core = core;
        _configfile = cfgloc;
        _config = conf;
        _procScheduler = new ProcedureScheduler(this, _config);
        _modules = new();
        _textcat = new(@$"{conf.DataDirectory}/ntextcat/languagemodel.xml");
        try
        {
            var tempdata = new SentinelDatabase($@"{conf.DataDirectory}/data.sqlite");
            tempdata.Database.EnsureCreated();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            throw;
        }
        _random = new Random();
        _assets = new AssetManager(@$"{conf.DataDirectory}/assets", @$"{conf.DataDirectory}/temp");
        _ocr = new OCRManager(@$"{conf.DataDirectory}/tessdata", this);
        _regexes = new Regexes();
    }

    private Task Log(LogMessage msg)
    {
        System.Diagnostics.Debug.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public DiscordSocketClient GetClient()
    {
        return _discord;
    }

    public async Task Start()
    {
        //SentinelEvents, Modules
        Events = new(this);
        LoadModules();

        var dcfg = new DiscordSocketConfig()
        {
            MessageCacheSize = 200,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.MessageContent,
            AlwaysDownloadUsers = true
        };

        //start
        _discord = new DiscordSocketClient(dcfg);
        //hook events
        _discord.Log += Log;
        _discord.Ready += Init;
        //setup automod
        _automod = new AutoMod(this, _discord);
        //setup detention
        _detention = new Detention(this, _discord);
        //setup twitter
        _twitter = new TwitterManager(this);
        //deleter
        _deleter = new MassDeleter(_discord);

        await _discord.LoginAsync(TokenType.Bot, _config.DiscordToken);
        await _discord.StartAsync();

        //create New Message handler
        _newMessageHandler = new(_discord, this, _ocr, _regexes, _detention, _config, _random, _textcat);

        //Hook remaining events
        HookEvents();

        //start ticking
        StartTicking();
    }

    private void LoadModules()
    {
        DirectoryInfo dir = new DirectoryInfo($@"{_config.DataDirectory}/plugins");
        foreach (var dll in dir.GetFiles("*.dll"))
        {
            System.Diagnostics.Debug.WriteLine(dll.FullName);
            Assembly assembly = Assembly.LoadFile(dll.FullName);
            Type[] plugintype = assembly.GetTypes().Where(type => typeof(ISentinelPlugin).IsAssignableFrom(type) && !type.IsInterface).ToArray();
            if (plugintype.Length != 1)
            {
                System.Diagnostics.Debug.WriteLine($"Found {plugintype.Length} ISentinelPlugin implementation in {dll.Name} when there should be 1");
                continue;
            }
            ISentinelPlugin? plugin = (ISentinelPlugin?)Activator.CreateInstance(plugintype[0]);
            if (plugin == null)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading {dll.Name}");
            }
            foreach (var module in plugin.Modules)
            {
                _modules.Add(module);
            }
        }

        foreach (var module in _modules)
        {
            System.Diagnostics.Debug.WriteLine("Loading " + module.GetType());
            module.ModuleLoad(this);
        }
    }

    public Config GetConfig()
    {
        return _config;
    }

    public async Task UpdateConfig()
    {
        await SentinelCore.WriteConfigAsync(_config, _configfile);
    }

    private async Task Init()
    {
        System.Diagnostics.Debug.WriteLine("Initialising");
        //setup interaction service
        var isc = new InteractionServiceConfig()
        {
            UseCompiledLambda = true
        };
        _interactions = new InteractionService(_discord);


        //setup dependency inj.
        var srv = new ServiceCollection();
        srv.AddSingleton(this);
        srv.AddSingleton(_automod);
        srv.AddSingleton(_assets);
        srv.AddSingleton(_ocr);
        srv.AddSingleton(_detention);
        srv.AddSingleton(_procScheduler);
        srv.AddSingleton(_twitter);
        srv.AddSingleton(_regexes);
        srv.AddSingleton(_deleter);
        srv.AddSingleton(_textcat);
        _services = srv.BuildServiceProvider();

        //add commands
        await _interactions.AddModuleAsync(typeof(SlapCommands), _services);
        await _interactions.AddModuleAsync(typeof(UtilityCommands), _services);
        await _interactions.AddModuleAsync(typeof(CensorCommands), _services);
        await _interactions.AddModuleAsync(typeof(ModerationCommands), _services);
        await _interactions.AddModuleAsync(typeof(ConfigCommands), _services);
        await _interactions.AddModuleAsync(typeof(PollCommand), _services);
        await _interactions.AddModuleAsync(typeof(OperationCommand), _services);
        await _interactions.AddModuleAsync(typeof(EconomyCommands), _services);
        await _interactions.AddModuleAsync(typeof(AdjustmentCommands), _services);
        await _interactions.AddModuleAsync(typeof(WipeCommands), _services);
        //await _interactions.AddModuleAsync(typeof(AudioCommands), _services);

        //reg commands
        foreach (ulong server in _config.Servers)
        {
            await _interactions.RegisterCommandsToGuildAsync(server);
        }

        //set status
        await _discord.SetGameAsync(_config.GetStatus(), type: ActivityType.Playing);

        //Update cached guild info
        var data = GetDbContext();
        foreach (var server in _discord.Guilds)
        {
            if (server == null) continue;
            ServerConfig servercfg = await data.GetServerConfig(server.Id);
            servercfg.Name = server.Name;
            servercfg.IconURL = server.IconUrl;
        }
        await data.SaveChangesAsync();
    }

    private void HookEvents()
    {
        //hook events for SentinelEvents
        _discord.MessageReceived += Events.MessageCreate;
        _discord.MessageDeleted += Events.MessageRemove;
        _discord.ReactionAdded += Events.ReactCreate;
        _discord.ReactionRemoved += Events.ReactRemove;
        _discord.MessageUpdated += Events.MessageAlter;
        _discord.MessagesBulkDeleted += Events.MessageRemoveBulk;

        //hook core events
        _discord.InteractionCreated += InteractionCreated;
        _discord.MessageReceived += _newMessageHandler.NewMessage;
        _discord.MessageUpdated += MessageEdit;
        _discord.ReactionAdded += NewReact;
        _discord.GuildMemberUpdated += UpdateMember;
        _discord.Connected += Reconnect;
        _discord.UserJoined += NewMember;
        _discord.ReactionRemoved += DelReact;
        _discord.UserIsTyping += Typing;
    }

    private async Task Typing(Cacheable<IUser, ulong> u, Cacheable<IMessageChannel, ulong> c)
    {
        IUser user = await u.GetOrDownloadAsync();
        IMessageChannel channel = await c.GetOrDownloadAsync();

        if (user is SocketGuildUser sgu)
        {
            SentinelDatabase data = GetDbContext();
            ServerUser su = await data.GetServerUser(sgu);

            if (su.SentinelAttitude == ServerUser.Attitude.Belligerent)
            {
                if (_random.Next(75) == 69)
                {
                    await sgu.SetTimeOutAsync(TimeSpan.FromSeconds(30));
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithDescription($"✅ {sgu.Mention} **was muted** | I felt like it");
                    eb.WithColor(Color.Green);
                    await channel.SendMessageAsync(embed: eb.Build());
                }
            }
        }
    }

    private async Task NewMember(SocketGuildUser arg)
    {
        var data = GetDbContext();
        var profile = await data.GetServerUser(arg);
        if (profile.Balance != 0)
        {
            int rejoinfee = 100;
            if (profile.Balance < 100) rejoinfee = profile.Balance;
            await data.Transact(profile, null, rejoinfee, Transaction.TxnType.Tax);

            if (arg.Guild.Id == 1019326226713817138)
            {
                var c = (ITextChannel)arg.Guild.GetChannel(1019326857193205770);
                await c.SendMessageAsync($"{arg.Mention} look who's back!");
                await c.SendMessageAsync("https://tenor.com/view/mr-burns-dont-forget-youre-here-forever-gif-18420566");
            }
        }
    }

    private async Task Reconnect()
    {
        await _discord.SetGameAsync(_config.GetStatus(), type: ActivityType.Playing);
    }

    private async Task MessageEdit(Cacheable<IMessage, ulong> arg1, SocketMessage msg, ISocketMessageChannel arg3)
    {
        var data = GetDbContext();
        //await _newMessageHandler.AntiChristmas(msg);

        if (msg.Author.IsBot) return;
        if (!(msg.Channel is SocketGuildChannel)) return;
        SocketGuildChannel channel = (SocketGuildChannel)msg.Channel;
        ServerUser user = await data.GetServerUser(msg.Author.Id, channel.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(channel.Guild.Id);

        if (user.Censored && srv.FunnyCommands)
        {
            if (CensorCheck(msg, srv))
            {
                await msg.DeleteAsync();
            }
        }

        //Apply Frenchification
        await NewMessageHandler.Francais(msg, user, srv, _textcat);

    }

    private async Task UpdateMember(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        var data = GetDbContext();
        if (!before.HasValue) return;
        string nBefore = before.Value.Nickname;
        string nAfter = after.Nickname;

        //await AntiSierraAktion(before, after);
        //await ProtectRoles(before, after,1019250206212116571);

        if (nBefore == nAfter) return;

        ServerUser user = await data.GetServerUser(after.Id, after.Guild.Id);
        ServerConfig srv = await data.GetServerConfig(after.Guild.Id);

        if (nAfter != null && nAfter.ToUpper().Contains("CHRISTMAS"))
        {
            await after.ModifyAsync(properties => properties.Nickname = "[REDACTED]");
            System.Diagnostics.Debug.WriteLine("Anti-Christmas Aktion");
        }

        if (user.Nicklock != "" && srv.FunnyCommands)
        {
            if (user.NicklockUntil <= DateTime.Now)
            {
                user.Nicklock = "";
                user.NicklockUntil = null;
                await data.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Ended lock");
                return;
            }
            if (nAfter == user.Nicklock) return;
            await after.ModifyAsync(properties => properties.Nickname = user.Nicklock);
            System.Diagnostics.Debug.WriteLine("Reverted nickchange");
        }
    }

    private static bool IsTimedOut(DateTimeOffset? timeout)
    {
        if (timeout == null) return false;
        if (timeout < DateTimeOffset.Now) return false;
        return true;
    }

    private async Task ProtectRoles(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after, ulong tamperid)
    {
        try
        {
            if (after.Guild.Id != 988430253972140062) return;
            var protectedRole = after.Guild.GetRole(tamperid);
            if (after.Roles.Contains(protectedRole) != before.Value.Roles.Contains(protectedRole))
            {
                bool tamper = false;
                await foreach (var entries in after.Guild.GetAuditLogsAsync(1))
                {
                    var entry = entries.First();
                    if (entry.Action == ActionType.MemberRoleUpdated)
                    {
                        if (entry.User.Id != 241325827810131978 && entry.User.Id != _discord.CurrentUser.Id)
                        {
                            System.Diagnostics.Debug.WriteLine("Role Tampering Detected");
                            tamper = true;
                        }
                    }
                }

                if (tamper)
                {
                    if (before.Value.Roles.Contains(protectedRole))
                    {
                        await after.AddRoleAsync(protectedRole);
                    }
                    else
                    {
                        await after.RemoveRoleAsync(protectedRole);
                    }
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            throw;
        }

    }

    public async Task DMBC5(string msg)
    {
        var bc5 = await _discord.GetUserAsync(241325827810131978);
        var dms = await bc5.CreateDMChannelAsync();
        await dms.SendMessageAsync(msg);
    }

    private void StartTicking()
    {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += Tick;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        _ticks++;
        var data = GetDbContext();
        //Every 30s
        if (_ticks % 30 == 0)
        {
            //PROCEDURE SCHEDULER
            try
            {
                await _procScheduler.Tick();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            //NICKLOCKS
            try
            {
                List<ServerUser> nicklocked = await data.Users.Where(usr => usr.Nicklock != "").ToListAsync();
                foreach (var user in nicklocked)
                {
                    if (user.NicklockUntil < DateTime.Now)
                    {
                        System.Diagnostics.Debug.WriteLine("Cleared Nicklock");
                        user.Nicklock = "";

                        string? newnick = user.PrevNick;
                        if (newnick != null && newnick == "") newnick = null;

                        var t1 = data.SaveChangesAsync();
                        var t2 = _discord.GetGuild(user.ServerSnowflake).GetUser(user.UserSnowflake)
                            .ModifyAsync(x => x.Nickname = newnick);
                        await t1;
                        await t2;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception in Nicklock Tick: " + e);
            }
        }

        //Every minute
        if (_ticks % 60 == 0)
        {
            _detention.Tick();
        }

        //Every 5 seconds
        if (_ticks % 5 == 3)
        {
            await _deleter.Delete();
        }

        //Every second
        await _ocr.TryNext();
    }


    private bool CensorCheck(SocketMessage msg, ServerConfig srv)
    {
        string msgcontent = msg.Content.ToUpper();
        foreach (CensorEntry censor in srv.Censor) if (censor.IsCensored(msgcontent)) return true;
        return false;
    }

    private async Task DelReact(Cacheable<IUserMessage, ulong> msgg, Cacheable<IMessageChannel, ulong> arg2, SocketReaction react)
    {
        var data = GetDbContext();
        IMessage msg;
        if (!react.Message.IsSpecified) msg = await react.Channel.GetMessageAsync(react.MessageId);
        else msg = react.Message.Value;
        await data.RemoveReact(react, msg);
    }

    private async Task NewReact(Cacheable<IUserMessage, ulong> msgg, Cacheable<IMessageChannel, ulong> b, SocketReaction react)
    {
        var data = GetDbContext();
        IMessage msg;
        if (!react.Message.IsSpecified) msg = await react.Channel.GetMessageAsync(react.MessageId);
        else msg = react.Message.Value;
        await data.AddReact(react, msg);

        //twitter thread thing
        Task.Run(async () => { await TwitterThread(react, msg); });
    }

    private async Task TwitterThread(SocketReaction react, IMessage msg)
    {
        if (react.Emote.Name == "🧵")
        {
            foreach (var reactentry in msg.Reactions)
            {
                if (!(reactentry.Value.IsMe && reactentry.Key.Name == "🧵"))
                {
                    var match = _regexes.TwitterId.Match(msg.Content);
                    if (match.Success)
                    {
                        System.Diagnostics.Debug.WriteLine("ID: " + match.Groups[1].Value);
                        var thread = await _twitter.GetThread(long.Parse(match.Groups[1].Value));
                        await msg.AddReactionAsync(new Emoji("🧵"));
                        List<Embed>? embeds = await _twitter.ThreadEmbed(thread);
                        if (embeds != null)
                        {
                            embeds.Reverse();
                            if (embeds.Count > 10)
                            {
                                while (embeds.Count > 10)
                                {
                                    embeds.Remove(embeds[0]);
                                }
                                await ((IUserMessage)msg).ReplyAsync("Last 10 Tweets Only", embeds: embeds.ToArray());
                            }
                            else
                            {
                                await ((IUserMessage)msg).ReplyAsync(embeds: embeds.ToArray());
                            }
                            //Get rid of embed
                            await msg.Channel.ModifyMessageAsync(msg.Id, m => m.Flags = msg.Flags | MessageFlags.SuppressEmbeds);
                        }
                    }
                }
            }
        }
    }
    private async Task InteractionCreated(SocketInteraction i)
    {
        try
        {
            var ctx = new SocketInteractionContext(_discord, i);
            var result = await _interactions.ExecuteCommandAsync(ctx, _services);

            if (!result.IsSuccess && result is PreconditionResult)
            {
                await i.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009", ephemeral: true);
            }

        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            await i.RespondAsync("Error");
        }
    }

    public class Regexes
    {
        public Regex NewIdiot;
        public Regex AddDays;
        public Regex TwitterId;

        public Regexes()
        {
            NewIdiot = new Regex(@"new <@&(\d+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            AddDays = new Regex(@"<@!?(\d+)> add (\d+) days?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            TwitterId = new Regex(@":\/\/twitter.com\/.+\/status\/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }

}