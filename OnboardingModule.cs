using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

namespace Sentinel;

[Group("onboarding","Commands for new user management")]
public class OnboardingModule : InteractionModuleBase
{
    private Data _data;
    private Detention _detention;
    
    public OnboardingModule(Data data, Detention detention)
    {
        _data = data;
        _detention = detention;
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setchannels","Set what channels messages should go to")]
    public async Task SetChannels(ITextChannel? general = null, ITextChannel? arrivals = null,
        ITextChannel? detention = null)
    {
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        if (general != null) srv.GeneralChannel = general.Id;
        if (arrivals != null) srv.ArrivalsChannel = arrivals.Id;
        if (detention != null) srv.IdiotChannel = detention.Id;
        await _data.SaveChangesAsync();
        await RespondAsync(":)", ephemeral: true);
    }
    
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setroles","Set roles for verification")]
    public async Task SetRoles(string rolestring)
    {
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        try
        {
            string[] roleStrings = rolestring.Split(",");
            ulong[] roles = new ulong[roleStrings.Length];
            for (int i = 0; i < roleStrings.Length; i++)
            {
                roles[i] = ulong.Parse(roleStrings[i]);
            }
            
            srv.DefaultRoles = rolestring;
            await _data.SaveChangesAsync();
            await RespondAsync(":)", ephemeral: true);
        }
        catch (Exception e)
        {
            await RespondAsync("Error parsing", ephemeral: true);
        }
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("setmessages","Modify welcome messages")]
    public async Task SetMessages()
    {
        await RespondWithModalAsync<MessageModal>("set_messages");
    }

    [ModalInteraction("set_messages", ignoreGroupNames: true)]
    public async Task ModalResponse(MessageModal modal)
    {
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        if (!string.IsNullOrEmpty(modal.Arrivals)) srv.ArrivalMessage = modal.Arrivals;
        if (!string.IsNullOrEmpty(modal.General)) srv.ApprovalMessage = modal.General;
        await _data.SaveChangesAsync();
        await RespondAsync(":)", ephemeral: true);
    }
    
    

    public class MessageModal : IModal
    {
        public string Title => "Edit Welcome Messages";
        
        [InputLabel("Arrivals message")]
        [ModalTextInput("arrivals", TextInputStyle.Paragraph)] 
        [RequiredInput(false)]
        public string? Arrivals { get; set; }
        
        [InputLabel("Approval message")]
        [ModalTextInput("general", TextInputStyle.Paragraph)] 
        [RequiredInput(false)]
        public string? General { get; set; }
        
    }

    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("massverify","Add all users with role to verification")]
    public async Task MassVerification(IRole role)
    {
        await DeferAsync();
        var users = await Context.Guild.GetUsersAsync();
        int verifications = 0;
        int already = 0;
        foreach (var u in users)
        {
            if (u != null && u.RoleIds.Contains(role.Id))
            {
                var su = await _data.GetServerUser(u);
                if (!su.Verified)
                {
                    su.Verified = true;
                    verifications++;
                }
                else
                {
                    already++;
                }
                    
            }
        }
        await _data.SaveChangesAsync();
        await FollowupAsync($"Verified {verifications} users. {already} were already verified.");
    }
    
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [ComponentInteraction("sentinel-verify-*", ignoreGroupNames: true)]
    public async Task Verify(ulong uid)
    {
        if(!((IGuildUser) Context.User).GuildPermissions.ModerateMembers)
        {
            await RespondAsync("This button is for moderators.", ephemeral: true);
            return;
        }
        
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        var usr = await _data.GetServerUser(uid, Context.Guild.Id);
        usr.Verified = true;
        _data.Entry(usr).Property(x => x.Verified).IsModified = true;
        await _data.SaveChangesAsync();
        var user = await Context.Guild.GetUserAsync(uid);
        await user.AddRolesAsync(srv.DeserialiseRoles());
        if (srv.GeneralChannel != null)
        {
            ITextChannel c = await Context.Guild.GetTextChannelAsync(srv.GeneralChannel.Value);
            await VerifyMessage(c,user,srv.ApprovalMessage,(IGuildUser) Context.User);
        }
        await _data.SaveChangesAsync();
        await RespondAsync($"Approved {user.Username}");
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [ComponentInteraction("sentinel-detain-*", ignoreGroupNames: true)]
    public async Task Detain(ulong uid)
    {
        if(!((IGuildUser) Context.User).GuildPermissions.ModerateMembers)
        {
            await RespondAsync("This button is for moderators.", ephemeral: true);
            return;
        }
        
        var srv = await _data.GetServerConfig(Context.Guild.Id);
        var usr = await _data.GetServerUser(uid, Context.Guild.Id);
        var user = await Context.Guild.GetUserAsync(uid);

        await _detention.ModifySentence(user, usr, srv, TimeSpan.FromDays(90));
        await _data.SaveChangesAsync();
        if (srv.IdiotChannel != null)
        {
            ITextChannel c = await Context.Guild.GetTextChannelAsync(srv.IdiotChannel.Value);
            await c.SendMessageAsync($"{user.Mention}: new <@&{srv.IdiotRole}>!");
        }
        await _data.SaveChangesAsync();
        await RespondAsync($"Detained {user.Username}");
    }
    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [ComponentInteraction("sentinel-kick-*", ignoreGroupNames: true)]
    public async Task Kick(ulong uid)
    {
        if(!((IGuildUser) Context.User).GuildPermissions.ModerateMembers)
        {
            await RespondAsync("This button is for moderators.", ephemeral: true);
            return;
        }
        
        var user = await Context.Guild.GetUserAsync(uid);
        await user.KickAsync();
        await RespondAsync($"Kicked {user.Username}");
    }

    
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    [SlashCommand("setverification","Set a user's verification status")]
    public async Task SetVerification(IUser user, bool? set = null)
    {
        var usr = await _data.GetServerUser(user.Id,Context.Guild.Id);
        if (set == null) usr.Verified = !usr.Verified;
        else usr.Verified = set.Value;
        await _data.SaveChangesAsync();
        await RespondAsync($"Verification {(usr.Verified ? "enabled":"disabled")} for {user.Mention}");
    }
    
    public static async Task UserJoin(Data data, SocketGuildUser user)
    {
        var srv = await data.GetServerConfig(user.Guild.Id);
        var usr = await data.GetServerUserNoCreate(user);

        if (usr == null)
        {
            //New User
            usr = await data.GetServerUser(user);
            if (srv.ArrivalsChannel != null)
            {
                ITextChannel c = user.Guild.GetTextChannel(srv.ArrivalsChannel.Value);
                await ArrivalMessage(c, user, user.Guild, srv.ArrivalMessage,false);
            }
        }
        else
        {
            //Returning User
            int rejoinfee = 100;
            if (usr.Balance < 100) rejoinfee = usr.Balance;
            await data.Transact(usr, null, rejoinfee, Transaction.TxnType.Tax);

            if (usr.IdiotedUntil > DateTime.Now)
            {
                if (srv.IdiotRole != null)
                {
                    await user.AddRoleAsync(srv.IdiotRole.Value);
                    
                    if (srv.IdiotChannel != null)
                    {
                        ITextChannel c = user.Guild.GetTextChannel(srv.IdiotChannel.Value);
                        await WelcomeBack(c, user);
                    }
                }
            }
            else
            {
                if (usr.Verified)
                {
                    ulong[] roles = srv.DeserialiseRoles();
                    await user.AddRolesAsync(roles);
                    if (srv.GeneralChannel != null)
                    {
                        ITextChannel c = user.Guild.GetTextChannel(srv.GeneralChannel.Value);
                        await WelcomeBack(c, user);
                    }
                }
                else
                {
                    if (srv.ArrivalsChannel != null)
                    {
                        ITextChannel c = user.Guild.GetTextChannel(srv.ArrivalsChannel.Value);
                        await ArrivalMessage(c, user, user.Guild, srv.ArrivalMessage,true);
                    }
                }
            }
        }

        await data.SaveChangesAsync();
    }

    public static async Task VerifyMessage(ITextChannel channel, IGuildUser user, string message, IGuildUser mod)
    {
        var eb = new EmbedBuilder();
        eb.WithTitle($"Welcome {user.DisplayName}!");
        eb.WithColor(0, 255, 0);
        eb.WithDescription(message);
        eb.WithFooter($"Access granted by {mod.DisplayName}", mod.GetAvatarUrl());
        await channel.SendMessageAsync(user.Mention, embed: eb.Build());
    }
    
    public static async Task ArrivalMessage(ITextChannel channel, IUser user, IGuild guild, string message, bool returning)
    {
        var eb = new EmbedBuilder();
        eb.WithTitle(returning ? $"Welcome back to {guild.Name}" : $"Welcome to {guild.Name}");
        eb.WithColor(0, 0, 255);
        eb.WithDescription(message);

        var buttons = new ComponentBuilder();
        buttons.WithButton("Verify", $"sentinel-verify-{user.Id}", ButtonStyle.Success, Emoji.Parse("✅"));
        buttons.WithButton("Detain", $"sentinel-detain-{user.Id}", ButtonStyle.Danger, Emoji.Parse("🔒"));
        buttons.WithButton("Kick", $"sentinel-kick-{user.Id}", ButtonStyle.Danger, Emoji.Parse("🚪"));
        await channel.SendMessageAsync(user.Mention,embed: eb.Build(), components: buttons.Build());
    }

    public static async Task WelcomeBack(ITextChannel channel, IUser user)
    {
        await channel.SendMessageAsync($"{user.Mention} look who's back!");
        await channel.SendMessageAsync("https://tenor.com/view/mr-burns-dont-forget-youre-here-forever-gif-18420566");
    }
}