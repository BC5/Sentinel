using System.Runtime.Serialization;
using Discord;
using Discord.WebSocket;

namespace Sentinel.Bot.Procedures;

[DataContract(Name = "ModifyServer")]
public class ServerModifyAction : ISentinelAction
{
    [DataMember]
    public ulong Server { get; set; }
    [DataMember]
    public byte[]? ServerImage { get; set; }
    [DataMember]
    public string? ServerName { get; set; }

    public async Task<ActionStatus> Execute(ProcedureContext context)
    {
        SocketGuild? server = context._discord.GetGuild(Server);

        if (server == null) return ActionStatus.CRITICAL_FAILURE;

        try
        {
            await server.ModifyAsync(srv =>
            {
                if (ServerImage != null) srv.Icon = new Image(new MemoryStream(ServerImage));
                if (ServerName != null) srv.Name = ServerName;
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return ActionStatus.FAILURE;
        }

        return ActionStatus.SUCCESS;
    }
}