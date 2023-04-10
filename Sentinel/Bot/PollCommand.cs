using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace Sentinel.Bot;

public class PollCommand : InteractionModuleBase
{
    private SentinelBot _bot;

    public PollCommand(SentinelBot bot)
    {
        _bot = bot;
    }

    [SlashCommand("anonpoll", "Create an anonymous poll")]
    public async Task Create(string title, string option1, string option2, string option3 = "", string option4 = "", string option5 = "")
    {
        List<string> options = new();
        options.Add(option1);
        options.Add(option2);
        if (option3 != "") options.Add(option3);
        if (option4 != "") options.Add(option4);
        if (option5 != "") options.Add(option5);

        for (int i = 0; i < options.Count; i++)
        {
            for (int j = 0; j < options.Count && j != i; j++)
            {
                if (options[i] == options[j])
                {
                    await RespondAsync("You can't have two identical options", ephemeral: true);
                    return;
                }
            }
        }

        var eb = new EmbedBuilder();
        eb.WithTitle(title);
        eb.WithFooter("Votes are anonymous, and cannot be changed once submitted.");

        var cpnt = new ComponentBuilder();
        var smbd = new SelectMenuBuilder();
        smbd.WithPlaceholder("Vote!");
        smbd.WithCustomId("sentinel-vote");
        smbd.WithMaxValues(1);
        smbd.WithMinValues(1);

        int k = 0;
        foreach (var opt in options)
        {
            eb.AddField(opt, "Votes: 0");
            smbd.AddOption(opt, $"sentinel-vote-{k}");
            k++;
        }
        cpnt.WithSelectMenu(smbd);
        await RespondAsync(embed: eb.Build(), components: cpnt.Build());
    }

    [ComponentInteraction("sentinel-vote")]
    public async Task Vote(string[] selection)
    {
        var data = _bot.GetDbContext();
        try
        {
            int i = int.Parse(selection[0].Replace("sentinel-vote-", ""));
            var msg = (SocketUserMessage)((IComponentInteraction)Context.Interaction).Message;

            bool voted = await data.CheckVoted(msg.Id, Context.User.Id);

            if (voted)
            {
                await RespondAsync("You can't vote twice or change your vote.", ephemeral: true);
                return;
            }

            await msg.ModifyAsync(x =>
            {
                var oldembed = msg.Embeds.First();
                var oldvotefield = oldembed.Fields[i].Value;
                Console.WriteLine(oldvotefield);
                int votes = int.Parse(oldvotefield.Replace("Votes: ", ""));
                votes++;

                var eb = new EmbedBuilder();
                eb.WithTitle(oldembed.Title);
                eb.WithFooter(oldembed.Footer?.Text);
                int j = 0;
                foreach (var field in oldembed.Fields)
                {
                    if (j == i)
                    {
                        eb.AddField(field.Name, $"Votes: {votes}");
                    }
                    else
                    {
                        eb.AddField(field.Name, field.Value);
                    }

                    j++;
                }
                x.Embed = eb.Build();
            });
            await data.RecordVote(msg.Id, Context.User.Id);
            await RespondAsync("Vote Recorded", ephemeral: true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}