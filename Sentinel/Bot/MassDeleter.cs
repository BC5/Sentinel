
using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

namespace Sentinel.Bot;

public class MassDeleter
{
    private DiscordSocketClient _discord;

    private Queue<IMessage> queue = new();
    private int DeletionsPerTick = 1;

    private IMessageChannel? _channel = null;

    public MassDeleter(DiscordSocketClient discord)
    {
        _discord = discord;
    }

    public void Enqueue(IMessage msg)
    {
        queue.Enqueue(msg);
    }

    public void SetChannel(IMessageChannel channel)
    {
        _channel = channel;
    }

    public bool GotChannel()
    {
        return _channel != null;
    }

    private async Task<bool> MoreMessages()
    {
        if (_channel == null) return false;
        var msgs = await _channel.GetMessagesAsync().ToListAsync();
        foreach (var roc in msgs)
        {
            foreach (var msg in roc)
            {
                queue.Enqueue(msg);
            }
        }
        return queue.Count > 0;
    }

    public async Task Delete()
    {
        if (queue.Count == 0)
        {
            if (_channel == null) return;
            if (!await MoreMessages())
            {
                Console.WriteLine("Channel Purge Complete");
                _channel = null;
                return;
            }
        }

        List<IMessage> delete = new();
        List<IMessage> bulkdel = new();
        List<Task> deletions = new();
        IMessage m;
        for (int i = 0; i < queue.Count; i++)
        {
            m = queue.Dequeue();
            if (DateTimeOffset.Now - m.Timestamp > TimeSpan.FromDays(14))
            {
                delete.Add(m);
            }
            else
            {
                bulkdel.Add(m);
            }

            if (delete.Count >= DeletionsPerTick) break;
        }

        if (bulkdel.Count > 0)
        {
            ILookup<IMessageChannel, IMessage> bulkDeletes = bulkdel.ToLookup(bd => bd.Channel, bd => bd);
            foreach (var job in bulkDeletes)
            {
                deletions.Add(((ITextChannel)job.Key).DeleteMessagesAsync(job.ToList()));
            }
        }

        foreach (var d in delete)
        {
            deletions.Add(d.DeleteAsync());
        }

        try
        {
            await Task.WhenAll(deletions);
        }
        catch (Exception e)
        {
            Console.WriteLine($"MassDeleter Exception: {e}");
        }

    }

}