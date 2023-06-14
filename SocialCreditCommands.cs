﻿using Discord;
using Discord.Interactions;
using FFMpegCore.Enums;

namespace Sentinel;

public class SocialCreditCommands : InteractionModuleBase
{

    private Data _data;
    
    public SocialCreditCommands(Data data)
    {
        _data = data;
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("socialcredit","荣耀归中共")]
    public async Task SocialCredit(IGuildUser target, long points, string reason)
    {
        if (points == 0)
        {
            await RespondAsync("Zero points? Really? That's pretty useless...", ephemeral: true);
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
            eb.AddField("Penalty", $"{Math.Abs(points):n0} Social Credits");
            eb.WithColor(100, 0, 0);
            eb.WithFooter($"YOUR SCORE IS NOW {su.SocialCredit:n0}. CLASSIFICATION: {FriendlyClassName(GetClass(su.SocialCredit)).ToUpper()}");
        }
        else
        {
            eb.WithTitle("SOCIAL CREDIT MERIT");
            eb.AddField("Commendation For",reason);
            eb.AddField("Reward", $"{Math.Abs(points):n0} Social Credits");
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