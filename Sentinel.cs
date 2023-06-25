using System.Data;
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
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Sentinel.Archivist;
using Sentinel.ImageProcessing;
using Sentinel.ImageProcessing.Operations;
using Sentinel.Logging;
using Sentinel.Procedures;
using IResult = Discord.Interactions.IResult;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PreconditionResult = Discord.Interactions.PreconditionResult;

namespace Sentinel;

public class Sentinel
{
    private SentinelLogging _log;
    
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
    private MessageManagement _messageManager;

    private ulong _ticks = 0;

    private List<ISentinelModule> _modules;
    public SentinelEvents Events { get; set; }
    private string _configfile = "";

    public static string SentinelVersion = "1.0.0";
    
    public static async Task Main()
    {
        //Execution dir
        string config1 = @"./sentinel.json";
        //Project dir (when working in Rider at least)
        string config2 = @"../../../sentinel.json";

        bool c1Exists = File.Exists(config1);
        bool c2Exists = File.Exists(config2);
        
        if (!c1Exists && !c2Exists)
        {
            Console.Error.WriteLine("No config file at " + Path.GetFullPath(config1));
            Console.Error.WriteLine("Making one for you. Put your discord token in it.");
            await WriteConfig(new Config(), config1);
            return;
        }

        //Prefer config in execution directory if both exist
        string cfgloc = config1;
        if (!c1Exists && c2Exists) cfgloc = config2;
        
        string json = await File.ReadAllTextAsync(cfgloc);
        Config? conf = JsonSerializer.Deserialize<Config>(json);

        if (conf == null)
        {
            Console.Error.WriteLine("Failed to read config");
            return;
        }

        if (conf.DiscordToken is null or "")
        {
            Console.Error.WriteLine("You need to put your discord token in the config file");
            return;
        }
        
        var sentinel = new Sentinel(conf,cfgloc);
        await sentinel.Start();
        
        //pause
        await Task.Delay(-1);
    }

    public SentinelLogging GetLogger()
    {
        return _log;
    }
    
    public Data GetDb()
    {
        return new Data($@"{_config.DataDirectory}/data.sqlite");;
    }

    public static async Task WriteConfig(Config conf, string file)
    {
        string jsontext = JsonSerializer.Serialize(conf,new JsonSerializerOptions() {WriteIndented = true});
        await File.WriteAllTextAsync(file,jsontext);
    }

    public Sentinel(Config conf, string cfgloc)
    {
        _log = new SentinelLogging();

        var console = new ConsoleOutput();
        console.SetLogLevel(LogType.Debug);
        _log.LogOutputs.Add(console);
        
        _configfile = cfgloc;
        _config = conf;
        _messageManager = new(this,_log);
        _procScheduler = new ProcedureScheduler(this,_config);
        _modules = new();
        _textcat = new(@$"{conf.DataDirectory}/ntextcat/languagemodel.xml");
        try
        {
            var tempdata = new Data($@"{conf.DataDirectory}/data.sqlite");
            tempdata.Database.EnsureCreated();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        _random = new Random();
        _assets = new AssetManager(@$"{conf.DataDirectory}/assets", @$"{conf.DataDirectory}/temp");
        _ocr = new OCRManager(@$"{conf.DataDirectory}/tessdata", this);
        _regexes = new Regexes();
    }

    private async Task Log(LogMessage msg)
    {
        LogEntry entry = new LogEntry(msg.Source, msg.Message, SentinelLogging.FromSeverity(msg.Severity));
        await _log.LogAsync(entry);
    }
    
    private async Task InteractionLog(LogMessage msg)
    {
        LogEntry entry = new LogEntry(msg.Source, msg.Message, SentinelLogging.FromSeverity(msg.Severity));
        await _log.LogAsync(entry);
    }

    public DiscordSocketClient GetClient()
    {
        return _discord;
    }
    
    public async Task Start()
    {
        //SentinelEvents, Modules
        Events = new(this);
        await LoadModules();
        
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
        _automod = new AutoMod(this,_discord);
        //setup detention
        _detention = new Detention(this, _discord);
        //setup twitter
        _twitter = new TwitterManager(this);
        //deleter
        _deleter = new MassDeleter(_discord);

        await _discord.LoginAsync(TokenType.Bot, _config.DiscordToken);
        await _discord.StartAsync();
        
        //create New Message handler
        _newMessageHandler = new(_discord, this, _ocr, _regexes, _detention, _config, _random, _textcat, _messageManager);
        
        //Hook remaining events
        HookEvents();
        
        //start ticking
        StartTicking();
    }

    private async Task LoadModules()
    {
        DirectoryInfo dir = new DirectoryInfo($@"{_config.DataDirectory}/plugins");
        foreach (var dll in dir.GetFiles("*.dll"))
        {
            Assembly assembly = Assembly.LoadFile(dll.FullName);
            Type[] plugintype = assembly.GetTypes().Where(type => typeof(ISentinelPlugin).IsAssignableFrom(type) && !type.IsInterface).ToArray();
            if (plugintype.Length != 1)
            {
                await _log.Error($"Core",$"Found {plugintype.Length} ISentinelPlugin implementation in {dll.Name} when there should be 1");
                continue;
            }
            ISentinelPlugin? plugin = (ISentinelPlugin?) Activator.CreateInstance(plugintype[0]);
            if (plugin == null)
            {
                await _log.Error($"Core",$"Error loading {dll.Name}");
            }
            foreach (var module in plugin.Modules)
            {
                _modules.Add(module);
            }
        }
        
        foreach (var module in _modules)
        {
            await _log.Info($"Core","Loading " + module.GetType());
            module.ModuleLoad(this);
        }
    }

    public Config GetConfig()
    {
        return _config;
    }

    public async Task UpdateConfig()
    {
        await WriteConfig(_config,_configfile);
    }

    private async Task Init()
    {
        await _log.Info("Core","Initialising");
        //setup interaction service
        var isc = new InteractionServiceConfig()
        {
            UseCompiledLambda = true
        };
        _interactions = new InteractionService(_discord, isc);
        

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
        srv.AddSingleton(_random);
        srv.AddSingleton(_log);
        
        //add dbcontext to dependency inj.
        string DbPath = $@"{_config.DataDirectory}/data.sqlite";
        srv.AddDbContext<Data>(o => o.UseSqlite($"Data Source={DbPath}"));
        
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
        await _interactions.AddModuleAsync(typeof(TaskHandler), _services);
        await _interactions.AddModuleAsync(typeof(ElectionCommand), _services);
        await _interactions.AddModuleAsync(typeof(RebuildCommand), _services);
        await _interactions.AddModuleAsync(typeof(CasinoModule), _services);
        await _interactions.AddModuleAsync(typeof(SocialCreditCommands), _services);
        await _interactions.AddModuleAsync(typeof(LeaderboardCommands), _services);
        await _interactions.AddModuleAsync(typeof(OnboardingModule), _services);
        //await _interactions.AddModuleAsync(typeof(AudioCommands), _services);
        
        //Hook interactions events
        _interactions.SlashCommandExecuted += SlashCommandExecuted;
        _interactions.Log += InteractionLog;

        //reg commands
        foreach (ulong server in _config.Servers)
        {
            await _interactions.RegisterCommandsToGuildAsync(server);
        }
        
        //set status
        await _discord.SetGameAsync(_config.GetStatus(), type: ActivityType.Playing);
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
        _discord.ModalSubmitted += Modal;
        _discord.MessageDeleted += Deleted;
        _discord.MessagesBulkDeleted += BulkDeleted;
    }

    private async Task BulkDeleted(IReadOnlyCollection<Cacheable<IMessage, ulong>> msgs, Cacheable<IMessageChannel, ulong> channel)
    {
        await _messageManager.MessagesRemove(msgs);
    }

    private async Task Deleted(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel)
    {
        using (var data = GetDb())
        {
            Task mm = _messageManager.MessageRemove(msg);
            var c = await channel.GetOrDownloadAsync();

            ITextChannel? logChannel = null;
            
            if (c is IGuildChannel gc)
            {
                var srv = await data.GetServerConfig(gc.GuildId);
                if (srv.LogChannel != null)
                {
                    logChannel = await gc.Guild.GetTextChannelAsync(srv.LogChannel.Value);
                }
            }
            await mm;

            if (logChannel != null && logChannel.Id != channel.Id)
            {
                if (msg.HasValue)
                {
                    var m = msg.Value;
                    var embed = new EmbedBuilder();
                    embed.WithDescription($"**Message Deleted by <@{m.Author.Id}> in <#{m.Channel.Id}>**\n{m.Content}");
                    embed.WithAuthor(m.Author);
                    embed.WithColor(Color.Red);
                    embed.WithFooter($"A: {m.Author.Id} M: {m.Id}");
                    await logChannel.SendMessageAsync(embed: embed.Build());
                }
                else
                {
                    var m = await _messageManager.GetMessage(msg.Id);
                    if (m == null)
                    {
                        var embed = new EmbedBuilder();
                        embed.WithDescription($"**Unknown message removed in <#{channel.Id}>**");
                        embed.WithColor(Color.Red);
                        embed.WithFooter($"M: {msg.Id}");
                        await logChannel.SendMessageAsync(embed: embed.Build());
                    }
                    else
                    {
                        var embed = new EmbedBuilder();
                        embed.WithDescription($"**Message Deleted by <@{m.AuthorId}> in <#{m.ChannelId}>**\n{m.Content}");
                        embed.WithColor(Color.Red);
                        embed.WithFooter($"A: {m.AuthorId} M: {m.MessageId}");
                        await logChannel.SendMessageAsync(embed: embed.Build());
                    }
                }
            }
            
        }
    }

    private async Task SlashCommandExecuted(SlashCommandInfo cmd, IInteractionContext ctx, IResult result)
    {
        if (!result.IsSuccess)
        {
            if (!ctx.Interaction.HasResponded)
            {
                await ctx.Interaction.RespondAsync("Something went wrong (many such cases)", ephemeral: true);
            }

            await _log.Error($"/{cmd.Name}", $"Error: {result.ErrorReason}");
        }
        return;
    }

    private async Task Modal(SocketModal smodal)
    {
        if (smodal.Data.CustomId.Contains("task-"))
        {
            await TaskHandler.Execute(smodal,_assets);
        }
    }

    private async Task Typing(Cacheable<IUser, ulong> u, Cacheable<IMessageChannel, ulong> c)
    {
        IUser user = await u.GetOrDownloadAsync();
        IMessageChannel channel = await c.GetOrDownloadAsync();

        if (user is SocketGuildUser sgu)
        {
            using (var data = GetDb())
            {
                ServerUser su = await data.GetServerUser(sgu);
                var creditclass = SocialCreditCommands.GetClass(su.SocialCredit);
                if (creditclass < SocialCreditCommands.CreditClass.Menace)
                {
                    if (_random.Next(60) == 26)
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
    }

    private async Task NewMember(SocketGuildUser arg)
    {
        using (var data = GetDb())
        {
            await OnboardingModule.UserJoin(data,arg);
            await data.SaveChangesAsync();
        }
    }

    private async Task Reconnect()
    {
        await _discord.SetGameAsync(_config.GetStatus(), type: ActivityType.Playing);
    }

    private async Task MessageEdit(Cacheable<IMessage, ulong> arg1, SocketMessage msg, ISocketMessageChannel arg3)
    {
        //await _newMessageHandler.AntiChristmas(msg);
        using (var data = GetDb())
        {
            if(!(msg.Channel is SocketGuildChannel)) return;
            SocketGuildChannel channel = (SocketGuildChannel) msg.Channel;
            ServerConfig srv = await data.GetServerConfig(channel.Guild.Id);
            
            if(srv.LogChannel != null && channel.Id != srv.LogChannel.Value)
            {
                var logChannel = channel.Guild.GetTextChannel(srv.LogChannel.Value);
                string before = "`unknown`";
                if (arg1.HasValue) before = arg1.Value.Content;
                else
                {
                    var dbm = await _messageManager.GetMessage(arg1.Id);
                    if (dbm != null) before = dbm.Content;
                }
                
                var embed = new EmbedBuilder();
                embed.WithDescription($"**Message Edited by <@{msg.Author.Id}> in <#{msg.Channel.Id}>**");
                embed.WithAuthor(msg.Author);
                embed.WithColor(255,255,0);

                embed.AddField("Before", before);
                embed.AddField("After", msg.Content);
                
                embed.WithFooter($"A: {msg.Author.Id} M: {msg.Id}");
                await logChannel.SendMessageAsync(msg.GetJumpUrl(),embed: embed.Build());
            }

            await _messageManager.MessageAlter(msg);
            if(msg.Author.IsBot) return;
            ServerUser user = await data.GetServerUser(msg.Author.Id, channel.Guild.Id);
            if (user.Censored && srv.FunnyCommands)
            {
                if (CensorCheck(msg, srv))
                {
                    await msg.DeleteAsync();
                }
            }
        
            //Apply Frenchification
            await NewMessageHandler.Francais(msg, user,srv,_textcat);
        }
        
    }

    private async Task UpdateMember(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        using (var data = GetDb())
        {
            if(!(before.HasValue)) return;
            string nBefore = before.Value.Nickname;
            string nAfter = after.Nickname;

            //await AntiSierraAktion(before, after);
            //await ProtectRoles(before, after,1019250206212116571);
        
            if(nBefore == nAfter) return;

            ServerUser user = await data.GetServerUser(after.Id, after.Guild.Id);
            ServerConfig srv = await data.GetServerConfig(after.Guild.Id);

            if (nAfter != null && nAfter.ToUpper().Contains("CHRISTMAS"))
            {
                await after.ModifyAsync(properties => properties.Nickname = "[REDACTED]");
            }

            if (user.Nicklock != "" && srv.FunnyCommands)
            {
                if (user.NicklockUntil <= DateTime.Now)
                {
                    user.Nicklock = "";
                    user.NicklockUntil = null;
                    await data.SaveChangesAsync();
                    await _log.Fine("Core",$"Nicklock for {after.Username} ended");
                    return;
                }
                if(nAfter == user.Nicklock) return;
                await after.ModifyAsync(properties => properties.Nickname = user.Nicklock);
                await _log.Fine("Core",$"Reverted {after.Username} nickname change");
            }
        }
    }

    private static bool IsTimedOut(DateTimeOffset? timeout)
    {
        if (timeout == null) return false;
        if (timeout < DateTimeOffset.Now) return false;
        return true;
    }
    private void StartTicking()
    {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += Tick;
        timer.AutoReset = true;
        timer.Enabled = true;
    }

    private async Task DoPurge(Data data)
    {
        SocketGuild? guild = null;
        PurgeConfiguration? pconf = null;
                
        foreach (var g in _discord.Guilds)
        {
            if (g == null)
            {
                await _log.Fine("Tick","null guild");
                continue;
            }
            var srv = await data.GetServerConfig(g.Id);
            foreach (var pc in srv.PurgeConfig)
            {
                if (DateTimeOffset.Now - pc.LastPurge > TimeSpan.FromDays(1))
                {
                    guild = g;
                    pconf = pc;
                    await _log.Fine("Tick", $"Eligible for purge {pc.ChannelID} : {(DateTimeOffset.Now - pc.LastPurge).Days} days ago");
                    break;
                }
                else
                {
                    await _log.Fine("Tick",$"{pc.ChannelID} : {(DateTimeOffset.Now - pc.LastPurge).Days} days ago");
                }
            }
            if(pconf != null) break;
        }

        if (pconf != null && guild != null)
        {
            pconf.LastPurge = DateTimeOffset.Now;
            ITextChannel channel = guild.GetTextChannel(pconf.ChannelID);

            List<ulong> purge = await _messageManager.GetMessagesToPurge(guild.Id, channel.Id);
            //Deduplicate
            purge = purge.Distinct().ToList();
            
            await _log.Info("Tick", $"Purging {purge.Count} messages from #{channel.Name} in {guild.Name}");
            
            try
            {
                var opt = new RequestOptions()
                {
                    AuditLogReason = "Automated Purge"
                };
                await channel.DeleteMessagesAsync(purge, opt);
            }
            catch (Exception e)
            {
                await _log.Error("Tick",$"Error purging {channel.Name} in {guild.Name}: {e}");
            }
        }
        else
        {
            await _log.Fine("Tick","No purges pending");
        }
    }
    
    private async void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        _ticks++;
        using (var data = GetDb())
        {
            //Once an hour
            Task? purgeTask = null;
            if (_ticks % 3600 == 50)
            {
                purgeTask = DoPurge(data);
            }
            
            
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
                    await _log.Error("Tick",$"Scheduled Procedure Failed: {e}");
                }
            
                //NICKLOCKS
                try
                {
                    List<ServerUser> nicklocked = await data.Users.Where(usr => usr.Nicklock != "").ToListAsync();
                    foreach (var user in nicklocked)
                    {
                        if (user.NicklockUntil < DateTime.Now)
                        {
                            await _log.Fine("Tick",$"Cleared nicklock for {user.CompositeID}");
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
                    await _log.Error("Tick",$"Nicklock Exception: {e}");
                }
            }
        
            //Every minute
            if (_ticks % 60 == 1)
            {
                _detention.Tick();
            }

            //Every 5 seconds
            if (_ticks % 5 == 3)
            {
                var r = Rebuild();
                await _deleter.Delete();
                await AnonPollUpdate();
                await r;
            }
        
            //Every second
            await _ocr.TryNext();
            await JuveChecks();
            
            //Await pending tasks
            if (purgeTask != null) await purgeTask;
            await data.SaveChangesAsync();
        }
    }

    public List<RebuildCommand.RebuildTask> RebuildTasks = new();
    private async Task Rebuild()
    {
        if (RebuildTasks.Count > 0)
        {
            if (RebuildTasks[0].IsComplete())
            {
                RebuildTasks.Remove(RebuildTasks[0]);
            }
            else
            {
                await RebuildTasks[0].Next();
            }
        }
    }
    

    private bool CensorCheck(SocketMessage msg, ServerConfig srv)
    {
        string msgcontent = msg.Content.ToUpper();
        foreach (CensorEntry censor in srv.Censor) if (censor.IsCensored(msgcontent)) return true;
        return false;
    }

    private async Task DelReact(Cacheable<IUserMessage, ulong> msgg, Cacheable<IMessageChannel, ulong> arg2, SocketReaction react)
    {
        using (var data = GetDb())
        {
            IMessage msg;
            if (!react.Message.IsSpecified) msg = await react.Channel.GetMessageAsync(react.MessageId);
            else msg = react.Message.Value;
            await data.RemoveReact(react,msg);
        }
    }
    
    private async Task NewReact(Cacheable<IUserMessage, ulong> msgg, Cacheable<IMessageChannel, ulong> b, SocketReaction react)
    {
        using (var data = GetDb())
        {
            IMessage msg;
            if (!react.Message.IsSpecified) msg = await react.Channel.GetMessageAsync(react.MessageId);
            else msg = react.Message.Value;
            await data.AddReact(react,msg);
        
            //twitter thread thing
            Task.Run(async () => { await TwitterThread(react, msg); });
        }
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
                                await ((IUserMessage) msg).ReplyAsync("Last 10 Tweets Only",embeds: embeds.ToArray());
                            }
                            else
                            {
                                await ((IUserMessage) msg).ReplyAsync(embeds: embeds.ToArray());
                            }
                            //Get rid of embed
                            await msg.Channel.ModifyMessageAsync(msg.Id, m => m.Flags = (msg.Flags | MessageFlags.SuppressEmbeds));
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
            if (_config.UserBlacklist.Contains(i.User.Id))
            {
                await i.RespondAsync("Your account has been blacklisted.",ephemeral: true);
                return;
            }
            
            var ctx = new SocketInteractionContext(_discord, i);
            var result = await _interactions.ExecuteCommandAsync(ctx, _services);

            if (!result.IsSuccess)
            {
                if (i is ISlashCommandInteraction sc)
                {
                    await _log.Error($"/{sc.Data.Name}",$"Errored: {result.ErrorReason}");
                }
                
                if (result is PreconditionResult)
                {
                    await i.RespondAsync("https://tenor.com/view/despicbable-me-minions-uh-no-no-eh-no-gif-3418009",ephemeral:true);
                }
            }
            
            
        }
        catch (Exception e)
        {
            await _log.Error($"Interaction",$"Errored: {e}");
            await i.RespondAsync("Error");
        }
    }

    public class Regexes
    {
        public Regex NewIdiot;
        public Regex AddDays;
        public Regex TwitterId;
        public Regex Candidates;
        public Regex VoteCount;
        
        public Regexes()
        {
            NewIdiot = new Regex(@"new <@&(\d+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            AddDays = new Regex(@"<@!?(\d+)> add (\d+) days?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            TwitterId = new Regex(@":\/\/twitter.com\/.+\/status\/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Candidates = new Regex(@"(?:Candidates: )((?:[A-Z]+[,\n])+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            VoteCount = new Regex(@"\*\*([\d]+)\*\* votes.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }

    public List<PendingAnonpollVote> PendingVotes = new();
    private async Task AnonPollUpdate()
    {
        List<ulong> messages = new();
        foreach (var v in PendingVotes)
        {
            if(!messages.Contains(v.PollId)) messages.Add(v.PollId);
        }
        
        foreach (var poll in messages)
        {
            var votes = PendingVotes.Where(x => x.PollId == poll).ToList();
            var channel = (ITextChannel) await _discord.GetChannelAsync(votes[0].PollChannel);
            var msg = (SocketUserMessage) await channel.GetMessageAsync(poll);
            
            var oldembed = msg.Embeds.First();

            Dictionary<int, int> v = new();
            foreach (var vote in votes)
            {
                if (v.ContainsKey(vote.Option)) v[vote.Option]++;
                else v.Add(vote.Option,1);
                PendingVotes.Remove(vote);
            }

            var eb = new EmbedBuilder();
            eb.WithTitle(oldembed.Title);
            eb.WithFooter(oldembed.Footer?.Text);
            int i = 0;
            foreach (var field in oldembed.Fields)
            {
                int tally = int.Parse(field.Value.Replace("Votes: ", ""));
                if (v.ContainsKey(i)) tally = tally + v[i];
                eb.AddField(field.Name, $"Votes: {tally}");
                i++;
            }
            
            await msg.ModifyAsync(x => x.Embed = eb.Build());
        }
    }

    public class PendingAnonpollVote
    {
        public PendingAnonpollVote(ulong pollid, ulong pollchannel, int option)
        {
            PollId = pollid;
            PollChannel = pollchannel;
            Option = option;
        }
        
        public ulong PollId;
        public ulong PollChannel;
        public int Option;
    }

    public List<IMessage> PendingJuvechecks = new();

    private async Task JuveChecks()
    {
        List<IMessage> Processed = new();
        foreach (var message in PendingJuvechecks)
        {
            Processed.Add(message);
            if(message is not SocketUserMessage) continue;
            var msg = (SocketUserMessage) message;
            await msg.ReplyAsync("Oops! Looks like you posted an image which did not contain the text \"JUVE\".");
            await msg.DeleteAsync();
        }

        foreach (var a in Processed)
        {
            PendingJuvechecks.Remove(a);
        }
    }

}