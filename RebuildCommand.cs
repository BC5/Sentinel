using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Sentinel.ImageProcessing;

namespace Sentinel;

public class RebuildCommand : InteractionModuleBase
{
    private Sentinel _core;
    
    public RebuildCommand(Sentinel core)
    {
        _core = core;
    }

    [Discord.Interactions.RequireOwner]
    [RequireUserPermission(GuildPermission.Administrator)]
    [SlashCommand("rebuild","Rebuild channel messages from chat log")]
    public async Task Rebuild(string log)
    {
        string logdir = $@"{_core.GetConfig().DataDirectory}/chatlogs";
        if (!Directory.Exists(logdir))
        {
            await RespondAsync("No chatlog directory. Place chatlogs in `/data/chatlogs` to rebuild them");
            return;
        }
        string logfile = $@"{_core.GetConfig().DataDirectory}/chatlogs/{log}.json";
        if (!File.Exists(logfile))
        {
            await RespondAsync("No chatlog json by that name");
            return;
        }

        await DeferAsync();

        string json = "";
        
        try
        {
            json = await File.ReadAllTextAsync(logfile);
        }
        catch (Exception e)
        {
            await FollowupAsync("Error reading from disk.");
            Console.WriteLine(e);
            return;
        }

        ChatLog? cl = null;
        try
        {
            cl = JsonSerializer.Deserialize<ChatLog>(json);
        }
        catch (Exception e)
        {
            await FollowupAsync("Error parsing JSON.");
            Console.WriteLine(e);
            return;
        }

        if (cl == null)
        {
            await FollowupAsync("Nothing parsed");
            return;
        }
        
        if (Context.Channel is ITextChannel itc)
        {
            var wh = await itc.CreateWebhookAsync("Sentinel Chat Rebuilder");
            RebuildTask task = new()
            {
                log = cl,
                webhook = wh,
                dir = logdir
            };
            _core.RebuildTasks.Add(task);
            await FollowupAsync($"Rebuilding #{cl.channel.name} from {cl.guild.name} here.\nThis should take around {(cl.messageCount*5)/60} minutes");
        }
        
    }

    public class RebuildTask
    {
        public ChatLog log;
        public IWebhook webhook;

        private DiscordWebhookClient? WebhookClient;
        
        public int i = 0;
        public string dir;

        public bool IsComplete()
        {
            return log.messageCount <= i;
        }

        private void SetupClient()
        {
            WebhookClient = new DiscordWebhookClient(webhook);
        }
        
        public async Task Next()
        {
            try
            {
                if(IsComplete()) return;
                Message msg = log.messages[i];
                i++;
                if(WebhookClient == null) SetupClient();
                if(WebhookClient == null) return;
            
                string avatar = msg.author.avatarUrl;
                string name = msg.author.name;
            
                if (msg.attachments.Count == 0)
                {
                    await WebhookClient.SendMessageAsync(msg.content, username: name, avatarUrl: avatar);
                }
                else
                {
                    List<FileAttachment> attachments = new();
                    foreach (var attachment in msg.attachments)
                    {
                        attachments.Add(new FileAttachment($@"{dir}/{attachment.url.Replace('\\','/')}"));
                    }
                    await WebhookClient.SendFilesAsync(attachments, msg.content, username: name, avatarUrl: avatar);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
    }


    public class ChatLog
    {
        public Guild guild { get; set; }
        public Channel channel { get; set; }
        public dateRange dateRange { get; set; }
        public List<Message> messages { get; set; }
        public int messageCount { get; set; }
    }

    public class Guild
    {
        public string id { get; set; }
        public string name { get; set; }
        public string iconurl { get; set; }
    }

    public class Channel
    {
        public string id { get; set; }
        public string type { get; set; }
        public string categoryId { get; set; }
        public string category { get; set; }
        public string name { get; set; }
        public string topic { get; set; }
    }

    public class dateRange
    {
        public string after { get; set; }
        public string before { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public string type { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public DateTimeOffset? timestampEdited { get; set; }
        public DateTimeOffset? callEndedTimestamp { get; set; }
        public bool isPinned { get; set; }
        public string content { get; set; }
        public User author { get; set; }
        public List<Attachment> attachments { get; set; }
        public List<Embed> embeds { get; set; }
        public List<Sticker> stickers { get; set; }
        public List<Reaction> reactions { get; set; }
        public List<User> mentions { get; set; }
        public Reference? reference { get; set; }
        
    }

    public class Reference
    {
        public string messageId { get; set; }
        public string channelId { get; set; }
        public string guildId { get; set; }
    }
    
    public class Sticker
    {
        public string id { get; set; }
        public string name { get; set; }
        public string format { get; set; }
        public string sourceUrl { get; set; }
    }
    
    public class Embed
    {
        public string title { get; set; }
        public string url { get; set; }
        public DateTimeOffset? timestamp { get; set; }
        public string description { get; set; }
        public Thumb thumbnail { get; set; }
        //images
        //fields
    }

    public class Reaction
    {
        public Emoji emoji { get; set; }
        public int count { get; set; }
    }

    public class Emoji
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public bool isAnimated { get; set; }
        public string imageUrl { get; set; }
    }

    public class Attachment
    {
        public string id { get; set; }
        public string url { get; set; }
        public string fileName { get; set; }
        public int fileSizeBytes { get; set; }
    }

    public class Thumb
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string name { get; set; }
        public string discriminator { get; set; }
        public string nickname { get; set; }
        public string color { get; set; }
        public bool isBot { get; set; }
        public string avatarUrl { get; set; }
    }
    
}