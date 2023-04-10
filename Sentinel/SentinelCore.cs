using Sentinel.Panel;
using Sentinel.Bot;
using System.Text.Json;

namespace Sentinel
{
    public class SentinelCore
    {

        public Config Config { get; set; }
        public WebPanel Panel { get; set; }
        public SentinelBot Bot { get; set; }

        public SentinelCore(Config conf, string confloc) 
        {
            Config = conf;
            Bot = new SentinelBot(Config,confloc,this);
            Panel = new WebPanel(this);
        }

        public async Task Start()
        {
            System.Diagnostics.Debug.WriteLine("SENTINEL CORE START");
            bool startpanel = true;
            bool startbot = false;

            Task? ptask = null;

            if(startpanel) ptask = Task.Run(() => { Panel.Start(); });
            
            if(startbot)
            {
                BotThread bthreadobj = new BotThread(Bot);
                Thread bthread = new Thread(new ThreadStart(bthreadobj.Run));
                bthread.Start();
                bthread.Join();
            }

            if (ptask != null) await ptask;

            //pause
            await Task.Delay(-1);   
        }

        public SentinelDatabase GetDbContext()
        {
            return new SentinelDatabase($@"{Config.DataDirectory}/data.sqlite");
        }

        public static void Main(string[] args)
        {
            (Config?,string?) cfg = getConfiguration();
            if (cfg.Item1 == null) return;
            if (cfg.Item2 == null) return;
            System.Diagnostics.Debug.WriteLine($"sentinel.json loaded from {Path.GetFullPath(cfg.Item2)}");
            var core = new SentinelCore(cfg.Item1, cfg.Item2);
            core.Start().Wait();
        }

        public static (Config?,string?) getConfiguration()
        {
            //Execution dir
            string config1 = @"./sentinel.json";
            //Project dir (when working in Rider at least)
            string config2 = @"../../../sentinel.json";

            bool c1Exists = File.Exists(config1);
            bool c2Exists = File.Exists(config2);

            if (!c1Exists && !c2Exists)
            {
                Console.Error.WriteLine("No config file at " + Path.GetFullPath(config1));
                Console.Error.WriteLine("Making one for you. Put your discord token in it.");
                WriteConfig(new Config(), config1);
                return (null,null);
            }

            //Prefer config in execution directory if both exist
            string cfgloc = config1;
            if (!c1Exists && c2Exists) cfgloc = config2;

            string json = File.ReadAllText(cfgloc);
            Config? conf = JsonSerializer.Deserialize<Config>(json);

            if (conf == null)
            {
                Console.Error.WriteLine("Failed to read config");
                return (null, null);
            }

            if (conf.DiscordToken is null or "")
            {
                Console.Error.WriteLine("You need to put your discord token in the config file");
                return (null, null);
            }

            return (conf,cfgloc);
        }

        public static void WriteConfig(Config conf, string file)
        {
            string jsontext = JsonSerializer.Serialize(conf, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(file, jsontext);
        }

        public static async Task WriteConfigAsync(Config conf, string file)
        {
            string jsontext = JsonSerializer.Serialize(conf, new JsonSerializerOptions() { WriteIndented = true });
            await File.WriteAllTextAsync(file, jsontext);
        }

    }

    public class BotThread
    {
        SentinelBot _bot;

        public BotThread(SentinelBot bot)
        {
            _bot = bot;
        }

        public void Run()
        {
            _ = this.RunAsync().Result;
        }

        public async Task<bool> RunAsync() 
        {
            await _bot.Start();
            await Task.Delay(-1);
            return true;
        }

    }

}