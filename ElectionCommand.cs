using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

[Group("election","Commands to do election stuff")]
public class ElectionCommand : InteractionModuleBase
{
    private Sentinel _sentinel;
    private Sentinel.Regexes _regexes;
    
    public ElectionCommand(Sentinel bot, Sentinel.Regexes rgx)
    {
        _sentinel = bot;
        _regexes = rgx;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setup","Set up election")]
    public async Task Setup(int options, string candidatecsv)
    {
        string msg = $"Candidates: {candidatecsv}\n";
        msg = msg + $"You have **{options}** votes. You cannot vote for the same candidate twice.";
        var builder = new ComponentBuilder().WithButton("Vote", $"stl-vote", emote: new Emoji("🗳️"));
        await Context.Channel.SendMessageAsync(msg, components: builder.Build());
        await RespondAsync("Done", ephemeral: true);
    }

    [ComponentInteraction("stl-vote",ignoreGroupNames:true)]
    public async Task VoteButton()
    {
        var d = (IComponentInteraction) Context.Interaction;
        var data = _sentinel.GetDbContext();
        var existing = await data.Ballots.FirstOrDefaultAsync(x => x.ElectionId == d.Message.Id && x.VoterId == Context.User.Id);
        if (existing != null)
        {
            await RespondAsync("You have already voted.",ephemeral: true);
        }
        await RespondWithModalAsync<BallotModal>($"stl-ballot-{d.Message.Id}");
    }

    [ModalInteraction("stl-ballot-*",ignoreGroupNames:true)]
    public async Task VoteModal(BallotModal modal, ulong id)
    {
        try
        {
            var msg = await Context.Channel.GetMessageAsync(id);
            string m = msg.Content;
            Console.WriteLine(_regexes.Candidates.Match(m).Groups[0].Value);
            string[] options = _regexes.Candidates.Match(m).Groups[0].Value.Split(",");
            int voteCount = int.Parse(_regexes.VoteCount.Match(m).Groups[0].Value);

            string[] ballot = modal.Vote.Split(",");
            if (ballot.Length != voteCount)
            {
                await RespondAsync($"Invalid ballot: You must vote for exactly {voteCount} candidates.",ephemeral: true);
                return;
            }

            for (int i = 0; i < ballot.Length; i++)
            {
                ballot[i] = ballot[i].ToUpper().Trim();
            }
            
            if (ballot.Distinct().Count() != ballot.Length)
            {
                await RespondAsync($"Invalid ballot: You have voted for a single candidate more than once", ephemeral: true);
                return;
            }
            
            foreach (string x in ballot)
            {
                if (!options.Contains(x))
                {
                    await RespondAsync($"Invalid ballot: {x} is not a valid option", ephemeral: true);
                    return;
                }
            }

            var ballotstring = String.Join(",", ballot);

            var data = _sentinel.GetDbContext();
            ElectionBallot b = new ElectionBallot();
            b.ElectionId = id;
            b.VoterId = Context.User.Id;
            b.Ballot = ballotstring;

            var existing = await data.Ballots.FirstOrDefaultAsync(x => x.ElectionId == b.ElectionId && x.VoterId == b.VoterId);
            if (existing != null)
            {
                await RespondAsync($"You have already voted.", ephemeral: true);
                return;
            }

            var db = data.Ballots.AddAsync(b);
            
            var srv = await data.GetServerConfig(Context.Guild.Id);
            if (srv.FlagChannel.HasValue)
            {
                var usr = Context.User;
                
                var channel = await Context.Guild.GetTextChannelAsync(srv.FlagChannel.Value);
                var eb = new EmbedBuilder();
                eb.WithAuthor(new EmbedAuthorBuilder().WithName(usr.Username).WithIconUrl(usr.GetAvatarUrl()));
                eb.WithTitle("Election Ballot");
                eb.WithDescription($"`{ballotstring}`");
                eb.WithColor(0, 255, 255);
                eb.WithFooter($"U:{usr.Id} E:{id}");
                await channel.SendMessageAsync(embed: eb.Build());
            }
            await db;
            await data.SaveChangesAsync();
            await RespondAsync("Vote Recorded",ephemeral: true);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public class BallotModal : IModal
    {
        public string Title => "Ballot";
        
        [InputLabel("Vote Here")]
        [ModalTextInput("vote",placeholder: "AAA,BBB,CCC")]
        public string Vote { get; set; }
    }

}