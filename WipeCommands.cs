using Discord;
using Discord.Interactions;

namespace Sentinel;

[DefaultMemberPermissions(GuildPermission.Administrator)]
[RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
[Group("wipe","Remove messages, en-masse")]
public class WipeCommands : InteractionModuleBase
{
    private MassDeleter _deleter;
    
    public WipeCommands(MassDeleter deleter)
    {
        _deleter = deleter;
    }

    [SlashCommand("channel","Delete all messages in this channel")]
    public async Task Channel(IMessageChannel channel)
    {
        if (_deleter.GotChannel())
        {
            await RespondAsync("I'm already wiping something. One thing at a time");
        }
        else
        {
            _deleter.SetChannel(channel);
            await RespondAsync($"Marked <#{channel.Id}> for deletion");
        }
        
    }
    
    
}