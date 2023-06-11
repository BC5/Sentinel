using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sentinel.Logging;
using SQLitePCL;

namespace Sentinel;

using Discord;
using Microsoft.EntityFrameworkCore;

public class MessageManagement
{

    private Sentinel _core;
    private SentinelLogging _log;
    public MessageManagement(Sentinel core, SentinelLogging log)
    {
        _core = core;
        _log = log;

        using (var db = GetLogDbContext())
        {
            try
            {
                db.Database.EnsureCreated();
            }
            catch (Exception e)
            {
                _log.Log(LogType.Error, "MsgMngr", $"Error creating new message database: {e}");
                throw;
            }
        }
    }
    
    public async Task MessageLog(IMessage msg)
    {
        using (var log = GetLogDbContext())
        {
            await log.AddMessage(msg);
            await log.SaveChangesAsync();
        }
    }

    public async Task MessageRemove(Cacheable<IMessage, ulong> msg, bool removed = false)
    {
        using (var log = GetLogDbContext())
        {
            await log.DeleteMessage(msg.Id, removed);
            await log.SaveChangesAsync();
        }
    }

    public async Task MessagesRemove(IEnumerable<Cacheable<IMessage, ulong>> msgs, bool removed = true)
    {
        using (var log = GetLogDbContext())
        {
            foreach (var msg in msgs)
            {
                await log.DeleteMessage(msg.Id, removed);
            }
            await log.SaveChangesAsync();
        }
    }
    
    public async Task MessageAlter(IMessage msg)
    {
        using (var log = GetLogDbContext())
        {
            
            await log.EditMessage(msg);
            await log.SaveChangesAsync();
        }
    }

    public async Task<List<ulong>> GetMessagesToPurge(ulong guild, ulong channel)
    {
        using (var log = GetLogDbContext())
        {
            ulong before = SnowflakeUtils.ToSnowflake(DateTimeOffset.Now - TimeSpan.FromDays(7));
            ulong after = SnowflakeUtils.ToSnowflake(DateTimeOffset.Now - TimeSpan.FromDays(13.9));
            var msgs = await log.MessageLog.Where(x => x.ServerId == guild && x.ChannelId == channel && x.MessageId < before && x.MessageId > after)
                .Select(x => x.MessageId).ToListAsync();
            return msgs;
        }
    }

    private Logs GetLogDbContext()
    {
        return new Logs($@"{_core.GetConfig().DataDirectory}/logs.sqlite");;
    }
    
    
    public class Logs : DbContext
    {
        public Logs(string file)
        {
            DbPath = file;
        }
        
        [NotMapped]
        public string DbPath { get; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder opt)
        {
            opt.UseSqlite($"Data Source={DbPath}");
        }
        
        public DbSet<Message> MessageLog { get; set; }

        public async Task AddMessage(IMessage msg)
        {
            Message message = new Message(msg);
            try
            {
                await MessageLog.AddAsync(message);
                await SaveChangesAsync();
            }
            catch (Exception e)
            {
                MessageLog.Remove(message);
                Console.WriteLine($"Error with message {message.Content}: {e}");
                try
                {
                    await SaveChangesAsync();
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Hard Error. Clearing {ChangeTracker.Entries().Count()} changes.");
                    Console.WriteLine(exception);
                    ChangeTracker.Clear();
                }
            }
        }

        public async Task EditMessage(IMessage msg)
        {
            Message? dbMessage = await MessageLog.Where(m => m.MessageId == msg.Id).SingleOrDefaultAsync();
            if(dbMessage == null) return;
            dbMessage.Content = msg.Content;
            await SaveChangesAsync();
        }

        public async Task DeleteMessage(ulong msgid, bool removed)
        {
            Message? dbMessage = await MessageLog.Where(m => m.MessageId == msgid).SingleOrDefaultAsync();
            if(dbMessage == null) return;
            dbMessage.Deleted = true;
            if(removed) dbMessage.Removed = true;
        }
        
        public class Message
        {
            [Key] public ulong MessageId { get; set; }
            public ulong ChannelId { get; set; }
            public ulong? ServerId { get; set; }
            public ulong AuthorId { get; set; }
            public ulong? ReplyId { get; set; }
            public string OriginalContent { get; set; } = "";
            public string Content { get; set; } = "";
            public string? Embeds { get; set; }
            public string? Attachments { get; set; }
            public bool Deleted { get; set; } = false;
            public bool Removed { get; set; } = false;

            public Message()
            {

            }

            public Message(IMessage msg)
            {
                MessageId = msg.Id;
                ChannelId = msg.Channel.Id;
                if (msg.Channel is IGuildChannel gc) ServerId = gc.GuildId;
                if (msg.Reference != null && msg.Reference.MessageId.IsSpecified)
                    ReplyId = msg.Reference.MessageId.Value;
                Content = msg.Content;
                OriginalContent = msg.Content;
                AuthorId = msg.Author.Id;

                if (msg.Embeds.Count > 0)
                {
                    Embeds = "";
                    foreach (var embed in msg.Embeds)
                    {
                        Embeds = Embeds + embed.Url + " ";
                    }
                }

                if (msg.Attachments.Count > 0)
                {
                    Attachments = "";
                    foreach (var attach in msg.Attachments)
                    {
                        Attachments = Attachments + attach.Url + " ";
                    }
                }

            }

        }
    }
    
}