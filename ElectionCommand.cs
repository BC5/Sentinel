﻿using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

[Group("election","Commands to do election stuff")]
public class ElectionCommand : InteractionModuleBase
{
    private Sentinel _sentinel;
    private Sentinel.Regexes _regexes;
    private Data _data;
    
    public ElectionCommand(Sentinel bot, Sentinel.Regexes rgx, Data data)
    {
        _sentinel = bot;
        _regexes = rgx;
        _data = data;
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
        try
        {
            var d = (IComponentInteraction) Context.Interaction;
            var existing = await _data.Ballots.FirstOrDefaultAsync(x => x.ElectionId == d.Message.Id && x.VoterId == Context.User.Id);
            if (existing != null)
            {
                await RespondAsync("You have already voted.",ephemeral: true);
                return;
            }
            await RespondWithModalAsync<BallotModal>($"stl-ballot-{d.Message.Id}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [ModalInteraction("stl-ballot-*",ignoreGroupNames:true)]
    public async Task VoteModal(ulong id, BallotModal modal) //MODAL OBJECT **MUST** BE LAST PARAM
    {
        try
        {
            await DeferAsync(ephemeral: true);
            
            var msg = await Context.Channel.GetMessageAsync(id);
            string m = msg.Content.ToUpper();
            var groups = _regexes.Candidates.Match(m).Groups;
            string[] options = _regexes.Candidates.Match(m).Groups[1].Value.Trim().Split(",");
            int voteCount = int.Parse(_regexes.VoteCount.Match(m).Groups[1].Value);
            string[] ballot = modal.Vote.Split(",");
            if (ballot.Length != voteCount)
            {
                await FollowupAsync($"Invalid ballot: You must vote for exactly {voteCount} candidates.",ephemeral: true);
                return;
            }

            for (int i = 0; i < ballot.Length; i++)
            {
                ballot[i] = ballot[i].ToUpper().Trim();
            }
            
            if (ballot.Distinct().Count() != ballot.Length)
            {
                await FollowupAsync($"Invalid ballot: You have voted for a single candidate more than once", ephemeral: true);
                return;
            }
            
            foreach (string x in ballot)
            {
                if (!options.Contains(x))
                {
                    await FollowupAsync($"Invalid ballot: {x} is not a valid option", ephemeral: true);
                    return;
                }
            }

            var ballotstring = String.Join(",", ballot);

            ElectionBallot b = new ElectionBallot();
            b.ElectionId = id;
            b.VoterId = Context.User.Id;
            b.Ballot = ballotstring;

            var existing = await _data.Ballots.FirstOrDefaultAsync(x => x.ElectionId == b.ElectionId && x.VoterId == b.VoterId);
            if (existing != null)
            {
                await FollowupAsync($"You have already voted.", ephemeral: true);
                return;
            }

            var db = _data.Ballots.AddAsync(b);
            
            var srv = await _data.GetServerConfig(Context.Guild.Id);
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
            await _data.SaveChangesAsync();
            await FollowupAsync("Vote Recorded",ephemeral: true);

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