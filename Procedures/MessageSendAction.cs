using System.Runtime.Serialization;
using Discord;

namespace Sentinel.Procedures;

[DataContract(Name = "MessageSend")]
public class MessageSendAction : ISentinelAction
{
    [DataMember]
    public ulong Channel { get; set; }
    [DataMember]
    public string Message { get; set; }
    
    public async Task<ActionStatus> Execute(ProcedureContext context)
    {
        IChannel? channel = context._discord.GetChannel(Channel);

        if (channel == null) return ActionStatus.FAILURE;
        if (channel is ITextChannel tchannel)
        {
            try
            {
                await tchannel.SendMessageAsync(Message);
                return ActionStatus.SUCCESS;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return ActionStatus.FAILURE;
            }
        }
        else
        {
            return ActionStatus.FAILURE;
        }
    }
}