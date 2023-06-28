using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
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

    public async Task<Logs.Message?> GetMessage(ulong id)
    {
        using (var log = GetLogDbContext())
        {
            return await log.FetchMessage(id);
        }
    }
    
    public async Task MessageLog(IMessage msg)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        using (var log = GetLogDbContext())
        {
            await log.AddMessage(msg);
            await log.SaveChangesAsync();
        }
        sw.Stop();
        Console.WriteLine($"Log writing took {sw.ElapsedMilliseconds:n0}ms");
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

    public async Task<List<ulong>> GetMessagesToPurge(ulong channel, ulong? before = null, ulong? after = null)
    {
        using (var log = GetLogDbContext())
        {
            if(before == null) before = SnowflakeUtils.ToSnowflake(DateTimeOffset.Now - TimeSpan.FromDays(7));
            if(after == null) after = SnowflakeUtils.ToSnowflake(DateTimeOffset.Now - TimeSpan.FromDays(13.9));
            List<Logs.Message> msgs = await GetMessagesBetween(log, channel, after.Value, before.Value);
            return msgs.Select(x => x.MessageId).ToList();
        }
    }

    public async Task<List<ulong>> GetChannelLastMessages(ulong channel, int count)
    {
        await using (var log = GetLogDbContext())
        {
            /*
            var x = await log.MessageLog.Where(x => x.ChannelId == channel && x.Deleted == false && x.Removed == false)
                .OrderByDescending(x => x.MessageId).Take(count)
                .Select(x => x.MessageId).ToListAsync();
            */

            var y = await log.MessageLog
                .FromSql($"SELECT * FROM MessageLog WHERE ChannelId = {channel} AND Deleted = false AND Removed = false ORDER BY MessageId DESC LIMIT {count}")
                .Select(x => x.MessageId).ToListAsync();
            return y;
        }
    }

    private Task<List<Logs.Message>> GetMessagesBetween(ulong channel, ulong after, ulong before)
    {
        using (var log = GetLogDbContext())
        {
            return GetMessagesBetween(log, channel, after, before);
        }
    }
    
    private async Task<List<Logs.Message>> GetMessagesBetween(Logs log, ulong channel, ulong after, ulong before)
    {
       return await log.MessageLog.FromSql(
                $"SELECT * FROM MessageLog WHERE ChannelId = {channel} AND MessageId BETWEEN {after} AND {before}")
            .ToListAsync();
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

        public async Task<Message?> FetchMessage(ulong id)
        {
            Message? dbMessage = await MessageLog.Where(m => m.MessageId == id).SingleOrDefaultAsync();
            return dbMessage;
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