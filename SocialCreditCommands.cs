using Discord;
using Discord.Interactions;
using FFMpegCore.Enums;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

public class SocialCreditCommands : InteractionModuleBase
{

    private Data _data;
    
    public SocialCreditCommands(Data data)
    {
        _data = data;
    }

    [SlashCommand("creditreport","Check a social credit score")]
    public async Task CreditReport(IGuildUser? target)
    {
        if (target == null) target = (IGuildUser) Context.User;

        var su = await _data.GetServerUser(target);
        var eb = new EmbedBuilder();
        eb.WithTitle($"Social Credit Report for {target.DisplayName}");

        eb.AddField("Social Credits",$"{su.SocialCredit:n0}",true);
        eb.AddField("Classification", FriendlyClassName(GetClass(su.SocialCredit)),true);
        
        var log = await _data.SocialCreditLog.Where(x => x.UserId == target.Id && x.ServerId == Context.Guild.Id).ToListAsync();
        foreach (var l in log.TakeLast(5))
        {
            eb.AddField($"{l.Points:n0} Credits", l.Reason);
        }
        eb.WithColor(0, 0, 125);
        eb.WithAuthor(target.DisplayName, target.GetDisplayAvatarUrl());

        await RespondAsync(embed: eb.Build());
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("socialcredit","荣耀归中共")]
    public async Task SocialCredit(IGuildUser target, long points, string reason, uint money = 0)
    {
        if (points == 0)
        {
            await RespondAsync("Zero points? Really? That's pretty useless...", ephemeral: true);
            return;
        }

        if (money > 1000)
        {
            await RespondAsync("You can't reward/fine more than £1000", ephemeral: true);
            return;
        }

        if (target.Id == Context.User.Id)
        {
            await RespondAsync("No self-reports.", ephemeral: true);
            return;
        }

        ServerUser su = await _data.GetServerUser(target);
        su.SocialCreditUpdate(_data,points,reason);
        await RespondAsync("Thank you for your report", ephemeral: true);
        var eb = new EmbedBuilder();
        if (points < 0)
        {
            eb.WithTitle("SOCIAL CREDIT INFRACTION");
            eb.AddField("Offense",reason);
            if (money != 0)
            {
                if (su.Balance < money)
                {
                    eb.AddField("Penalty", $"{Math.Abs(points):n0} Social Credits\nFine of £{money:n0} (You don't have enough. We'll just take everything you've got)");
                    money = (uint) su.Balance;
                }
                else
                {
                    eb.AddField("Penalty", $"{Math.Abs(points):n0} Social Credits\nFine of £{money:n0}");
                }
                await _data.Transact(su, null, (int) money, Transaction.TxnType.SocialCredit);
            }
            else
            {
                eb.AddField("Penalty", $"{Math.Abs(points):n0} Social Credits");
            }
            eb.WithColor(100, 0, 0);
            eb.WithFooter($"YOUR SCORE IS NOW {su.SocialCredit:n0}. CLASSIFICATION: {FriendlyClassName(GetClass(su.SocialCredit)).ToUpper()}");
        }
        else
        {
            eb.WithTitle("SOCIAL CREDIT MERIT");
            eb.AddField("Commendation For",reason);
            if (money != 0)
            {
                eb.AddField("Reward", $"{Math.Abs(points):n0} Social Credits\nCash Reward of £{money:n0}");
                await _data.Transact(null, su, (int) money, Transaction.TxnType.SocialCredit);
            }
            else
            {
                eb.AddField("Reward", $"{Math.Abs(points):n0} Social Credits");
            }
            eb.WithColor(0, 180, 0);
            eb.WithFooter($"YOUR SCORE IS NOW {su.SocialCredit:n0}. CLASSIFICATION: {FriendlyClassName(GetClass(su.SocialCredit)).ToUpper()}");
        }
        await _data.SaveChangesAsync();
        await Context.Channel.SendMessageAsync(target.Mention, embed: eb.Build());
    }
    
    

    public static string FriendlyClassName(CreditClass c)
    {
        switch (c)
        {
            case CreditClass.EnemyOfState:
                return "Enemy of the State";
            case CreditClass.Scum:
                return "Scum";
            case CreditClass.Menace:
                return "Antisocial Menace";
            case CreditClass.Nuisance:
                return "Antisocial Nuisance";
            case CreditClass.Neutral:
                return "NPC";
            case CreditClass.Good:
                return "Citizen";
            case CreditClass.Upstanding:
                return "Upstanding Citizen";
            case CreditClass.Honourable:
                return "Honourable";
            case CreditClass.Angelic:
                return "Angelic";
            default:
                return "ERROR";
        }
    }


    public static CreditClass GetClass(long score)
    {
        CreditClass c = CreditClass.EnemyOfState;
        if (score >= -10000) c = CreditClass.Scum;
        if (score >= -5000) c = CreditClass.Menace;
        if (score >= -500) c = CreditClass.Nuisance;
        if (score >= -50) c = CreditClass.Neutral;
        if (score >= 250) c = CreditClass.Good;
        if (score >= 1000) c = CreditClass.Upstanding;
        if (score >= 5000) c = CreditClass.Honourable;
        if (score >= 10000) c = CreditClass.Angelic;
        return c;
    }

    public enum CreditClass
    {
        EnemyOfState = -4,
        Scum = -3,
        Menace = -2,
        Nuisance = -1,
        Neutral = 0,
        Good = 1,
        Upstanding = 2,
        Honourable = 3,
        Angelic = 4
    }
    
}