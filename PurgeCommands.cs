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
    
    [SlashCommand("last","Purge last messages in channel")]
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
}