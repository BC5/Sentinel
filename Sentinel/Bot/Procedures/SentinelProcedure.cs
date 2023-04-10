using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Sentinel.Bot.Procedures;

[DataContract(Name = "Procedure")]
[KnownType(typeof(ChannelModifyAction))]
[KnownType(typeof(ServerModifyAction))]
[KnownType(typeof(MessageSendAction))]
[KnownType(typeof(SentinelAvatarAction))]
public class SentinelProcedure : ISentinelAction
{
    [DataMember]
    public List<ISentinelAction> Actions { get; set; } = new();
    private ProcedureContext? _context;

    public SentinelProcedure(SentinelBot bot)
    {
        _context = new ProcedureContext(bot);
    }

    public SentinelProcedure()
    {

    }

    public string Serialise()
    {
        DataContractSerializer xml = new DataContractSerializer(GetType());
        MemoryStream xmlstream = new MemoryStream();
        xml.WriteObject(xmlstream, this);
        string xmlstring = Encoding.UTF8.GetString(xmlstream.ToArray());
        return xmlstring;
    }

    public static SentinelProcedure Deserialise(string xmlstring, SentinelBot bot)
    {
        DataContractSerializer xml = new DataContractSerializer(typeof(SentinelProcedure));
        object? proc = xml.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(xmlstring)));
        if (proc == null || proc.GetType() != typeof(SentinelProcedure))
        {
            throw new Exception("XML Parsing Error");
        }

        SentinelProcedure procc = (SentinelProcedure)proc;
        procc._context = new ProcedureContext(bot);
        return procc;
    }

    public Task<ActionStatus> Execute()
    {
        return Execute(_context);
    }
    public async Task<ActionStatus> Execute(ProcedureContext context)
    {
        ActionStatus procstatus = ActionStatus.SUCCESS;
        foreach (ISentinelAction action in Actions)
        {
            ActionStatus status = await action.Execute(context);
            switch (status)
            {
                case ActionStatus.CRITICAL_FAILURE:
                    return ActionStatus.CRITICAL_FAILURE;
                case ActionStatus.FAILURE:
                    procstatus = ActionStatus.FAILURE;
                    break;
            }
        }
        return procstatus;
    }
}