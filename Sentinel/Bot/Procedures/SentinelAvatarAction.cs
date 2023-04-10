using System.Runtime.Serialization;
using Discord;

namespace Sentinel.Bot.Procedures;

[DataContract(Name = "AvatarChange")]
public class SentinelAvatarAction : ISentinelAction
{

    [DataMember]
    public byte[]? AvatarImage { get; set; }
    public async Task<ActionStatus> Execute(ProcedureContext context)
    {
        try
        {
            await context._discord.CurrentUser.ModifyAsync(self => self.Avatar = new Image(new MemoryStream(AvatarImage)));
            return ActionStatus.SUCCESS;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return ActionStatus.FAILURE;
        }
    }
}