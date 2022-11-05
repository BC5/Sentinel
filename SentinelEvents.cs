using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Sentinel;

public class SentinelEvents
{
    private Sentinel _bot;

    public SentinelEvents(Sentinel bot)
    {
        _bot = bot;
    }
    
    public event Func<MessageContext, Task> NewMessage;
    public event Func<MessageRemoveContext, Task> RemoveMessage;
    public event Func<ReactContext, Task> NewReact;
    public event Func<ReactContext, Task> RemoveReact;
    public event Func<MessageEditContext, Task> AlterMessage;


    public async Task MessageRemove(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel)
    {
        Func<MessageRemoveContext, Task> handler = RemoveMessage;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");
            MessageRemoveContext ctx = await MessageRemoveContext.Create(msg, channel, db);
            try
            {
                await handler(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await db.SaveChangesAsync();
        }
    }
    public async Task MessageCreate(SocketMessage msg)
    {
        Func<MessageContext, Task> handler = NewMessage;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");
            MessageContext ctx = await MessageContext.Create(msg,db);
            try
            {
                await handler(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await db.SaveChangesAsync();
        }
    }
    public async Task ReactCreate(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
    {
        Func<ReactContext, Task> handler = NewReact;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");
            ReactContext ctx = await ReactContext.Create(react,await msg.GetOrDownloadAsync(),db,false);
            try
            {
                await handler(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await db.SaveChangesAsync();
        }
    }

    public async Task ReactRemove(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
    {
        Func<ReactContext, Task> handler = RemoveReact;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");
            ReactContext ctx = await ReactContext.Create(react,await msg.GetOrDownloadAsync(),db,true);
            try
            {
                await handler(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await db.SaveChangesAsync();
        }
    }

    public async Task MessageAlter(Cacheable<IMessage, ulong> msgold, SocketMessage msgnew, ISocketMessageChannel channel)
    {
        Func<MessageEditContext, Task> handler = AlterMessage;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");
            MessageEditContext ctx = await MessageEditContext.Create(msgnew,msgold,db);
            try
            {
                await handler(ctx);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await db.SaveChangesAsync();
        }
    }

    public async Task MessageRemoveBulk(IReadOnlyCollection<Cacheable<IMessage, ulong>> msgs, Cacheable<IMessageChannel, ulong> channel)
    {
        Func<MessageRemoveContext, Task> handler = RemoveMessage;
        if (handler != null)
        {
            Data db = new Data($@"{_bot.GetConfig().DataDirectory}\data.sqlite");

            foreach (var msg in msgs)
            {
                MessageRemoveContext ctx = await MessageRemoveContext.Create(msg, channel, db);
                ctx.BulkRemoved = true;
                try
                {
                    await handler(ctx);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            await db.SaveChangesAsync();
        }
    }
}

public class MessageEditContext
{
    public static async Task<MessageEditContext> Create(SocketMessage msg, Cacheable<IMessage, ulong> old, Data data)
    {
        MessageEditContext ctx = new MessageEditContext();
        ctx.Message = msg;
        ctx.OldMessage = old;
        ctx.DatabaseContext = data;
        if (msg.Author is IGuildUser gu)
        {
            ctx.UserProfile = await data.GetServerUser(gu);
            ctx.ServerConfig = await data.GetServerConfig(gu.GuildId);
            ctx.DataLoaded = true;
        }
        return ctx;
    }

    public SocketMessage Message { get; set; }
    public Cacheable<IMessage, ulong> OldMessage { get; set; }
    public bool DataLoaded = false;
    public ServerUser UserProfile { get; set; }
    public ServerConfig ServerConfig { get; set; }
    public Data DatabaseContext { get; set; }
}

public class ReactContext
{
    public static async Task<ReactContext> Create(SocketReaction react, IUserMessage message, Data data, bool remove)
    {
        ReactContext ctx = new ReactContext();
        ctx.React = react;
        ctx.Message = message;
        ctx.DatabaseContext = data;
        ctx.Remove = remove;
        if (react.User.Value is IGuildUser gu)
        {
            ctx.ReactorProfile = await data.GetServerUser(gu);
            ctx.ReacteeProfile = await data.GetServerUser(message.Author.Id, gu.GuildId);
            ctx.ServerConfig = await data.GetServerConfig(gu.GuildId);
            ctx.DataLoaded = true;
        }
        return ctx;
    }

    public bool Remove { get; set; }
    public bool DataLoaded = false;
    public SocketReaction React { get; set; }
    public IUserMessage Message { get; set; }
    public ServerUser ReactorProfile { get; set; }
    public ServerUser ReacteeProfile { get; set; }
    public ServerConfig ServerConfig { get; set; }
    public Data DatabaseContext { get; set; }
}

public class MessageContext
{
    public static async Task<MessageContext> Create(SocketMessage msg, Data data)
    {
        MessageContext ctx = new MessageContext();
        ctx.Message = msg;
        ctx.DatabaseContext = data;
        if (msg.Author is IGuildUser gu)
        {
            ctx.UserProfile = await data.GetServerUser(gu);
            ctx.ServerConfig = await data.GetServerConfig(gu.GuildId);
            ctx.DataLoaded = true;
        }
        return ctx;
    }

    public SocketMessage Message { get; set; }
    public bool DataLoaded = false;
    public ServerUser UserProfile { get; set; }
    public ServerConfig ServerConfig { get; set; }
    public Data DatabaseContext { get; set; }
}
public class MessageRemoveContext
{
    public static async Task<MessageRemoveContext> Create(Cacheable<IMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, Data data)
    {
        MessageRemoveContext ctx = new MessageRemoveContext();
        ctx.Message = msg;
        ctx.Channel = channel;
        ctx.DatabaseContext = data;
        if (channel.HasValue && channel.Value is IGuildChannel gc)
        {
            ctx.ServerConfig = await data.GetServerConfig(gc.GuildId);
            if (msg.HasValue)
            {
                ctx.UserProfile = await data.GetServerUser(msg.Value.Author.Id,gc.GuildId);
            }
            ctx.DataLoaded = true;
        }
        return ctx;
    }

    public Cacheable<IMessage, ulong> Message { get; set; }
    public Cacheable<IMessageChannel, ulong> Channel { get; set; }
    public bool BulkRemoved { get; set; } = false;
    public bool DataLoaded = false;
    public ServerUser UserProfile { get; set; }
    public ServerConfig ServerConfig { get; set; }
    public Data DatabaseContext { get; set; }
}