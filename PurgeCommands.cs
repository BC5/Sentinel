using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;

namespace Sentinel;

[RequireUserPermission(GuildPermission.Administrator)]
[Group("purge","Remove messages en-masse")]
public class PurgeCommands : InteractionModuleBase
{
    private MessageManagement _mmgr;
    
    public PurgeCommands(MessageManagement mmgr)
    {
        _mmgr = mmgr;
    }
    
    [SlashCommand("last","Purge last x messages in channel")]
    public async Task Last(int count = 100)
    {
        if (Context.Channel is ITextChannel itc)
        {
            await DeferAsync();
            var msgs= await _mmgr.GetChannelLastMessages(Context.Channel.Id, count);
            await itc.DeleteMessagesAsync(msgs);
            await FollowupAsync($"Removed {msgs.Count:n0} messages");
        }
        else
        {
            await RespondAsync("Can't purge this type of channel",ephemeral: true);
        }
    }

    [SlashCommand("between","Purge messages between two points or after a certain point")]
    public async Task Between([Summary(description: "Message ID or Message Link to begin purge from (inclusive)")] string after,
        [Summary(description: "Message ID or Message Link to end purge at (inclusive)")] string? before = null)
    {
        ulong? afterid = ParseMessageReference(after);
        ulong? beforeid = ParseMessageReference(before);

        if (afterid == null)
        {
            await RespondAsync("Invalid message reference for After",ephemeral: true);
            return;
        }
        if (before != null)
        {
            if (beforeid == null)
            {
                await RespondAsync("Invalid message reference for Before",ephemeral: true);
                return;
            }
            if (beforeid < afterid)
            {
                await RespondAsync("Message reference for Before must not be earlier than the one for After", ephemeral: true);
                return;
            }
        }
        await DeferAsync();
        var amsg = await Context.Channel.GetMessageAsync(afterid.Value);
        if (amsg == null)
        {
            await FollowupAsync("Invalid message reference for After");
            return;
        }
        if (beforeid != null)
        {
            var bmsg = await Context.Channel.GetMessageAsync(beforeid.Value);
            if (bmsg == null)
            {
                await FollowupAsync("Invalid message reference for Before");
                return;
            }
        }

        if (Context.Channel is ITextChannel itc)
        {
            var msgs = await _mmgr.GetChannelMessagesBetween(itc.Id, afterid.Value, beforeid);
            await itc.DeleteMessagesAsync(msgs);
            await FollowupAsync($"Removed {msgs.Count:n0} messages");
        }
        else
        {
            await FollowupAsync("Can't purge this type of channel", ephemeral: true);
            return;
        }

    }


    private static ulong? ParseMessageReference(string? str)
    {
        if (str == null) return null;
        
        ulong ul;
        bool parsed = ulong.TryParse(str, out ul);
        if(parsed) return ul;

        Regex rx = new Regex(@"discord.com\/channels\/\d+\/\d+\/(\d+)", RegexOptions.IgnoreCase);
        MatchCollection mc = rx.Matches(str);

        if (mc.Count > 0)
        {
            parsed = ulong.TryParse(mc[0].Groups[1].Value, out ul);
            if (parsed) return ul;
        }
        
        return null;
    }

}