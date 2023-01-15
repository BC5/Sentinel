using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Sentinel;

public class Detention
{
    private Sentinel _core;
    private DiscordSocketClient _discord;
    
    public Detention(Sentinel core, DiscordSocketClient discord)
    {
        _core = core;
        _discord = discord;
    }

    public async void Tick()
    {
        try
        {
            var data = _core.GetDbContext();
            var expired = await data.Users.Where(x => x.IdiotedUntil != null && x.IdiotedUntil < DateTime.Now).ToListAsync();
            foreach (var user in expired)
            {
                if (!_discord.Guilds.Any(x => x.Id == user.ServerSnowflake))
                {
                    continue;
                };
                
                if (user.IdiotedUntil < DateTime.Now)
                {
                    IGuildUser u = _discord.GetGuild(user.ServerSnowflake).GetUser(user.UserSnowflake);
                    ServerConfig scfg = await data.GetServerConfig(user.ServerSnowflake);
                    if(scfg.IdiotRole == null) continue;
                    await Unidiot(u, user, scfg.IdiotRole.Value);
                    await data.SaveChangesAsync();
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task Unidiot(IGuildUser gu, ServerUser su, ulong idiotRole)
    {
        su.IdiotedUntil = null;
        if(gu == null) return;
        
        await gu.RemoveRoleAsync(idiotRole);
        ulong[] roles = DeserialiseRoles(su.RoleBackup);
        if (roles.Length > 0)
        {
            await gu.AddRolesAsync(roles);
        }
    }

    public async Task Idiot(IGuildUser gu, ServerUser su, ulong idiotRole)
    {
        List<ulong> rolelist = gu.RoleIds.ToList();
        
        //Get server IRoles to reference against user's Role IDs
        Dictionary<ulong, IRole> serverroles = new Dictionary<ulong, IRole>();
        foreach (var r in gu.Guild.Roles)
        {
            serverroles.Add(r.Id,r);
        }

        //Ignore all managed roles
        List<ulong> managed = new List<ulong>();
        foreach (var r in rolelist) if (serverroles[r].IsManaged) managed.Add(r);
        foreach (ulong m in managed) rolelist.Remove(m);
        //Ignore @everyone
        rolelist.Remove(gu.GuildId);

        ulong[] roles = rolelist.ToArray();
        su.RoleBackup = SerialiseRoles(roles);
        if (roles.Length > 0)
        {
            await gu.RemoveRolesAsync(roles);
        }
        await gu.AddRoleAsync(idiotRole);
    }

    public static string SerialiseRoles(ulong[] roles)
    {
        string rolestr = "";
        for (int i = 0; i < roles.Length; i++)
        {
            rolestr = rolestr + roles[i];
            if (i < roles.Length - 1) rolestr = rolestr + ',';
        }
        return rolestr;
    }

    public static ulong[] DeserialiseRoles(string rolestr)
    {
        string[] rolestrs = rolestr.Split(',',StringSplitOptions.RemoveEmptyEntries);
        ulong[] roles = new ulong[rolestrs.Length];
        for (int i = 0; i < rolestrs.Length; i++)
        {
            roles[i] = ulong.Parse(rolestrs[i]);
        }
        return roles;
    }

    public async Task ModifySentence(IMessage msg, SocketGuildChannel channel, TimeSpan duration, Data data)
    {
        var author = msg.Author;
        IGuildUser author2;
        if (author is IGuildUser igu)
        {
            author2 = igu;
        }
        else
        {
            author2 = channel.Guild.GetUser(author.Id);
        }
        await ModifySentence(author2, duration, data);
    }
    
    public async Task ModifySentence(IGuildUser gu, TimeSpan duration, Data data)
    {
        ServerUser su = await data.GetServerUser(gu.Id, gu.GuildId);
        ServerConfig scfg = await data.GetServerConfig(gu.GuildId);
        await ModifySentence(gu, su, scfg, duration);
        await data.SaveChangesAsync();
    }
    
    public async Task ModifySentence(IGuildUser gu, ServerUser u, ServerConfig scfg, TimeSpan duration)
    {
        if (scfg.IdiotRole == null)
        {
            Console.WriteLine("Attempted to idiot with no idiot role set");
            return;
        }

        if (gu.IsBot)
        {
            Console.WriteLine("Attempted to idiot a bot (breaks things)");
            return;
        }
        
        //For null values
        if (u.IdiotedUntil == null)
        {
            if (duration > TimeSpan.Zero)
            {
                u.IdiotedUntil = DateTime.Now + duration;
                await Idiot(gu, u, scfg.IdiotRole.Value);
            }
            return;
        }
        
        if (u.IdiotedUntil > DateTime.Now)
        {
            //Already idioted
            u.IdiotedUntil = u.IdiotedUntil + duration;
            if (duration < TimeSpan.Zero)
            {
                //Check if now released
                if (u.IdiotedUntil < DateTime.Now)
                {
                    //Release
                    u.IdiotedUntil = null;
                    await Unidiot(gu, u, scfg.IdiotRole.Value);
                }
            }
            else
            {
                //Double check we've given them the role
                if (!gu.RoleIds.Contains(scfg.IdiotRole.Value))
                {
                    await Idiot(gu, u, scfg.IdiotRole.Value);
                }
            }
        }
        else
        {
            if(duration < TimeSpan.Zero) return;
            
            //New idioting
            u.IdiotedUntil = DateTime.Now + duration;
            await Idiot(gu, u, scfg.IdiotRole.Value);
        }
    }
    
    
    
}