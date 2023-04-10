using Discord;
using Discord.Interactions;

namespace Sentinel.Bot.Audio;

public class AudioCommands : InteractionModuleBase
{
    [SlashCommand("join","Get Sentinel to join a voice channel",runMode: RunMode.Async)]
    public async Task Join(IVoiceChannel channel)
    {
        var client = await channel.ConnectAsync();
        var test = new AudioTest(client, Context.Guild.Id);
        await RespondAsync("✅");
        Console.WriteLine(Environment.CurrentDirectory);
    }
}