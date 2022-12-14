namespace Sentinel.Procedures;

public class ProcedureScheduler
{

    private Config _config;
    private Sentinel _bot;

    public ProcedureScheduler(Sentinel bot, Config conf)
    {
        _config = conf;
        _bot = bot;
    }

    public async Task Add(ScheduledProcedure proc)
    {
        _config.ProcedureSchedule.Add(proc);
        _config.ProcedureSchedule.Sort();
        await _bot.UpdateConfig();
    }

    public async Task Tick()
    {
        if (_config.ProcedureSchedule.Count > 0)
        {
            ScheduledProcedure next = _config.ProcedureSchedule[0];
            if (next.ProcedureTrigger <= DateTime.Now)
            {
                string procpath = $@"{_bot.GetConfig().DataDirectory}/procedures/{next.ProcedureName}.xml";
                if (!File.Exists(procpath))
                {
                    Console.WriteLine("Failed to execute scheduled procedure: No file");
                    return;
                }
                string xmlstr = await File.ReadAllTextAsync(procpath);
                try
                {
                    SentinelProcedure procedure = SentinelProcedure.Deserialise(xmlstr,_bot);
                    ActionStatus status = await procedure.Execute();
                    Console.WriteLine($"`PROCEDURE-{next.ProcedureName}` EXECUTION {status}");
                    _config.ProcedureSchedule.Remove(next);
                    await _bot.UpdateConfig();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }
            }
        }
    }

    public class ScheduledProcedure : IComparable
    {
        public string ProcedureName { get; set; }
        public DateTime ProcedureTrigger { get; set; }
        
        public int CompareTo(object? obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj is ScheduledProcedure proc2)
            {
                return ProcedureTrigger.CompareTo(proc2.ProcedureTrigger);
            }
            else throw new ArgumentException("Can't compare ScheduledProcedure and " + obj.GetType());
        }
    }
}