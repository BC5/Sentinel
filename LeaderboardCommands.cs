using Discord;
using Discord.Interactions;

namespace Sentinel;

[Group("leaderboard","See leaderboards")]
public class LeaderboardCommands : InteractionModuleBase
{

    private Data _data;
    
    public LeaderboardCommands(Data data)
    {
        _data = data;
    }
    
    [SlashCommand(name: "balance", description: "Highest bank balances")]
    public async Task Balance(int count = 10, bool bottom = false)
    {
        var top10 = _data.GetTopBalance(Context.Guild.Id,count,bottom);

        EmbedBuilder eb = new();
        eb.WithTitle(bottom ? "Bottom Users by Balance" : "Top Users by Balance");
        eb.WithColor(0xFFF700);
        int i = 1;
        string leaderboard = "";
        foreach (var user in top10)
        {
            if (user.ServerSnowflake != user.UserSnowflake)
            {
                leaderboard = leaderboard + $"**{i}** - <@{user.UserSnowflake}> - £{user.Balance:n0}\n";
            }
            else
            {
                leaderboard = leaderboard + $"**{i}** - Server - £{user.Balance:n0}\n";
            }
            i++;
        }
        eb.WithDescription(leaderboard);

        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }
    
    [SlashCommand(name: "score", description: "Highest lifetime earnings")]
    public async Task Score(int count = 10, bool bottom = false)
    {
        var top10 = _data.GetTopScore(Context.Guild.Id,count,bottom);

        EmbedBuilder eb = new();
        eb.WithTitle(bottom ? "Bottom Users by Score" : "Top Users by Score");
        eb.WithColor(0xFFF700);
        int i = 1;
        string leaderboard = "";
        foreach (var user in top10)
        {
            if (user.ServerSnowflake != user.UserSnowflake)
            {
                leaderboard = leaderboard + $"**{i}** - <@{user.UserSnowflake}> - {user.Earnings:n0}\n";
            }
            else
            {
                leaderboard = leaderboard + $"**{i}** - Server - {user.Earnings:n0}\n";
            }
            i++;
        }
        eb.WithDescription(leaderboard);

        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }
    
    [SlashCommand(name: "credit", description: "Highest social credit")]
    public async Task Leaderboard(int count = 10, bool bottom = false)
    {
        var top10 = _data.GetTopCredit(Context.Guild.Id,count,bottom);

        EmbedBuilder eb = new();
        eb.WithTitle(bottom ? "Bottom Users by Social Credit" : "Top Users by Social Credit");
        eb.WithColor(0xFFF700);
        int i = 1;
        string leaderboard = "";
        foreach (var user in top10)
        {
            if (user.ServerSnowflake != user.UserSnowflake)
            {
                leaderboard = leaderboard + $"**{i}** - <@{user.UserSnowflake}> - {user.SocialCredit:n0}\n";
            }
            else
            {
                leaderboard = leaderboard + $"**{i}** - Server - {user.SocialCredit:n0}\n";
            }
            i++;
        }
        eb.WithDescription(leaderboard);

        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }

    [SlashCommand(name: "react", description: "Highest amount of a given reaction")]
    public async Task Reacts(string react, int count = 10, bool bottom = false, bool given = false)
    {
        List<(ulong id, int count)> top10;
        if (given) top10 = _data.GetTopReactGiven(Context.Guild.Id,react,count,bottom);
        else top10 = _data.GetTopReactReceived(Context.Guild.Id,react,count,bottom);
        EmbedBuilder eb = new();
        if(given) eb.WithTitle(bottom ? $"Bottom Users by {react} reacts given" : $"Top Users by {react} reacts given");
        else eb.WithTitle(bottom ? $"Bottom Users by {react} reacts received" : $"Top Users by {react} reacts received");
        eb.WithColor(0xFFF700);
        int i = 1;
        string leaderboard = "";
        foreach (var user in top10)
        {
            leaderboard = leaderboard + $"**{i}** - <@{user.id}> - {user.count:n0}\n";
            i++;
        }
        eb.WithDescription(leaderboard);

        await RespondAsync(embed: eb.Build(), ephemeral: true);
    }
    
}