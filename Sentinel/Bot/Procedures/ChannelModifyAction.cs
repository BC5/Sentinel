using System.Runtime.Serialization;
using Discord;
using Discord.WebSocket;

namespace Sentinel.Bot.Procedures;

[DataContract(Name = "ModifyChannel")]
public class ChannelModifyAction : ISentinelAction
{
    [DataMember]
    public ulong Server { get; set; }
    [DataMember]
    public ulong Channel { get; set; }
    [DataMember]
    public string? ChannelName { get; set; }
    [DataMember]
    public string? ChannelDesc { get; set; }
    [DataMember]
    public int? Slowmode { get; set; }
    [DataMember]
    public ulong? Category { get; set; }
    [DataMember]
    public int? Position { get; set; }


    public async Task<ActionStatus> Execute(ProcedureContext context)
    {
        SocketGuild? server = context._discord.GetGuild(Server);
        if (server == null) return ActionStatus.CRITICAL_FAILURE;
        SocketGuildChannel? channel = server.GetChannel(Channel);
        if (channel == null) return ActionStatus.FAILURE;

        try
        {
            ChannelType? type = channel.GetChannelType();
            switch (type)
            {
                case ChannelType.Text:
                    await ((SocketTextChannel)channel).ModifyAsync(chn =>
                    {
                        if (ChannelName != null) chn.Name = ChannelName;
                        if (ChannelDesc != null) chn.Topic = ChannelDesc;
                        if (Slowmode != null) chn.SlowModeInterval = Slowmode.Value;
                        if (Category != null) chn.CategoryId = Category.Value;
                        if (Position != null) chn.Position = Position.Value;
                    });
                    break;
                default:
                    await channel.ModifyAsync(chn =>
                    {
                        if (ChannelName != null) chn.Name = ChannelName;
                        if (Category != null) chn.CategoryId = Category.Value;
                        if (Position != null) chn.Position = Position.Value;
                    });
                    break;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return ActionStatus.FAILURE;
        }

        return ActionStatus.SUCCESS;

    }
}