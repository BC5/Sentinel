using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ImageMagick;
using Microsoft.EntityFrameworkCore;
using Tesseract;

namespace Sentinel.Archivist;

public class OCRManager
{
    private SHA1 _sha;
    private Sentinel _core;
    private HttpClient _http;
    private TesseractEngine _tess;

    private Task? _lastTask;
    private Queue<IMessage> _messageQueue;

    private IMessageChannel? _indexing;
    private IMessage? _lastMessageIndexed;
    private IUser _indexingInvoker;
    private DateTime _indexingStart;
    
    public OCRManager(string tessdata, Sentinel core)
    {
        _core = core;
        _sha = SHA1.Create();
        _http = new();
        _tess = TessSetup(tessdata, "eng");
        _messageQueue = new Queue<IMessage>();
    }

    private TesseractEngine TessSetup(string tessdata, string lang)
    {
        try
        {
            return new TesseractEngine(tessdata, lang);
        }
        catch (TargetInvocationException e)
        {
            if (e.InnerException is DllNotFoundException)
            {
                Console.WriteLine("Couldn't find libraries");
                if (File.Exists("/.dockerenv"))
                {
                    Console.WriteLine("We're in a docker container");
                    Console.WriteLine("libleptonica found: "  + File.Exists("/app/x64/libleptonica-1.80.0.so"));
                    Console.WriteLine("libtesseract found: "  + File.Exists("/app/x64/libtesseract41.so"));
                }
            }
            
            throw;
        }
    }

    public async Task FetchMoreMessages()
    {
        if (_indexing != null)
        {
            IAsyncEnumerable<IReadOnlyCollection<IMessage>> msgEnumerable;
            if (_lastMessageIndexed == null)
            {
                msgEnumerable = _indexing.GetMessagesAsync();
            }
            else
            {
                msgEnumerable = _indexing.GetMessagesAsync(fromMessage: _lastMessageIndexed, Direction.Before);
            }
            IMessage[] msgs = (await msgEnumerable.FlattenAsync()).ToArray();
            if(msgs == null) return;
            if(_indexing == null) return;
            Console.WriteLine($"Fetched {msgs.Length} more messages from #{_indexing.Name}");
            if (msgs.Length == 0)
            {
                await _indexing.SendMessageAsync(
                    $"{_indexingInvoker.Mention} indexing completed in {(DateTime.Now - _indexingStart):mm\\:ss}");
                _indexing = null;
                _lastMessageIndexed = null;
            }
            
            foreach (var msg in msgs)
            {
                if (msg.Attachments is {Count: > 0} || msg.Embeds is {Count: > 0})
                {
                    EnqueueMessage(msg);
                }
                _lastMessageIndexed = msg;
            }
        }
    }

    public bool BeginIndexing(IMessageChannel channel, IUser invoker)
    {
        if (_indexing == null)
        {
            _indexing = channel;
            _lastMessageIndexed = null;
            _indexingInvoker = invoker;
            _indexingStart = DateTime.Now;
            return true;
        }
        return false;
    }

    public Task<List<OCREntry>> QueryIndex(string q, ulong channel)
    {
        var data = _core.GetDbContext();
        return data.OcrEntries.FromSqlInterpolated($"SELECT Id,Server,Channel,Message,ImageURL,ImageHash,OE.Text FROM OcrIndex JOIN OcrEntries OE on OcrIndex.Text = OE.Text WHERE OcrIndex.Text MATCH {q} AND OE.Channel = {channel};").ToListAsync();
    }

    public void EnqueueMessage(IMessage msg)
    {
        _messageQueue.Enqueue(msg);
    }

    public async Task TryNext()
    {
        if (_lastTask == null || _lastTask.IsCompleted)
        {
            if (_messageQueue.Count > 0)
            {
                IMessage msg = _messageQueue.Dequeue();
                _lastTask = Task.Run((() => ProcessMessage(msg)));
            }
            else
            {
                if (_indexing != null)
                {
                    await FetchMoreMessages();
                }
            }
        }
    }

    public async Task ProcessMessage(IMessage msg)
    {
        if (msg.Channel is IGuildChannel channel)
        {
            var data = _core.GetDbContext();
            var results = await data.OcrEntries.Where(m => m.Message == msg.Id).ToListAsync();
            if (results.Count > 0)
            {
                Console.WriteLine("Already indexed message. Skipping.");
                await PostProcess(msg);
                return;
            }
            
            foreach (var attachment in msg.Attachments)
            {
                if (attachment.ContentType is "image/png" or "image/jpeg" or "image/webp")
                {
                    await DoOCR(msg, channel, attachment.Url);
                }
            }

            foreach (var embed in msg.Embeds)
            {
                if (embed.Type == EmbedType.Image)
                {
                    await DoOCR(msg, channel, embed.Url);
                }
                else if(embed.Image.HasValue)
                {
                    await DoOCR(msg, channel, embed.Image.Value.Url);
                }
            }
            
            await PostProcess(msg);
        }
    }

    public async Task PostProcess(IMessage msg)
    {
        var data = _core.GetDbContext();
        var results = await data.OcrEntries.Where(m => m.Message == msg.Id).ToListAsync();
        var su = await data.GetServerUser(msg.Author.Id, ((IGuildChannel) msg.Channel).GuildId);

        if (su.Juvecheck)
        {
            foreach (var r in results)
            {
                if (!r.Text.ToUpper().Contains("JUVE"))
                {
                    _core.PendingJuvechecks.Add(msg);
                    break;
                }
            }
        }
    }
    
    public async Task DoOCR(IMessage msg, IGuildChannel channel, string imgurl)
    {

        try
        {
            if(await MatchURL(imgurl, msg, channel.Id)) return;

            HttpResponseMessage? response = null;
            try
            {
                response = await _http.GetAsync(imgurl);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Downloading {imgurl} gave response of {response.StatusCode}");
                return;
            }

            Console.WriteLine("Downloaded");
            byte[] imagedata = await response.Content.ReadAsByteArrayAsync();

            if (response.Content.Headers.ContentType == null)
            {
                Console.WriteLine("No ContentType on " + imgurl);
                return;
            }
            string? mimetype = response.Content.Headers.ContentType.MediaType;
            if (mimetype == null)
            {
                Console.WriteLine("No MIME Type on " + imgurl);
                return;
            }

            if(await MatchHash(imagedata, imgurl, msg, channel.GuildId)) return;

            MagickImage image;
            switch (mimetype)
            {
                case "image/png":
                    image = new MagickImage(imagedata, MagickFormat.Png);
                    break;
                case "image/jpeg":
                    image = new MagickImage(imagedata, MagickFormat.Jpeg);
                    break;
                case "image/webp":
                    image = new MagickImage(imagedata, MagickFormat.WebP);
                    break;
                default:
                    Console.WriteLine($"Unsupported MIME Type {mimetype}");
                    return;
            }
        
            //Tesseract works best with black text on a white background.
            //This should invert dark mode screenshots so it works properly
            double brightness = image.Statistics().Composite().Mean;
            brightness = (1.0 / Quantum.Max) * brightness;
            if (brightness > 0.35)
            {
                image.Negate(Channels.RGB);
            }
            var piximage = Pix.LoadFromMemory(image.ToByteArray(MagickFormat.Png));
            var page = _tess.Process(piximage);
            string text = page.GetText();
            page.Dispose();
            piximage.Dispose();

            OCREntry newentry = new OCREntry()
            {
                Message = msg.Id,
                Channel = channel.Id,
                Server = channel.GuildId,
                ImageHash = _sha.ComputeHash(imagedata),
                ImageURL = imgurl,
                Text = text
            };

            var data = _core.GetDbContext();
            await data.OcrEntries.AddAsync(newentry);
            await data.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    

    public async Task<bool> MatchURL(string url, IMessage msg, ulong serverid)
    {
        var data = _core.GetDbContext();
        List<OCREntry> entries = await data.OcrEntries.Where(x => x.ImageURL == url).ToListAsync();

        if (entries.Count == 0) return false;
        OCREntry entry = entries[0];
        
        if (entry != null)
        {
            OCREntry newentry = new OCREntry()
            {
                Message = msg.Id,
                Channel = msg.Channel.Id,
                ImageHash = entry.ImageHash,
                ImageURL = url,
                Text = entry.Text,
                Server = serverid
            };
            await data.OcrEntries.AddAsync(newentry);
            await data.SaveChangesAsync();
            Console.WriteLine("Matched URL");
            return true;
        }

        return false;
    }
    
    public async Task<bool> MatchHash(byte[] data, string url, IMessage msg, ulong serverid)
    {
        byte[] hash = _sha.ComputeHash(data);
        var db = _core.GetDbContext();
        OCREntry? entry = await db.OcrEntries.Where(x => x.ImageHash == hash).SingleOrDefaultAsync();

        if (entry != null)
        {
            OCREntry newentry = new OCREntry()
            {
                Message = msg.Id,
                Channel = msg.Channel.Id,
                ImageHash = hash,
                ImageURL = url,
                Text = entry.Text,
                Server = serverid
            };
            await db.OcrEntries.AddAsync(newentry);
            await db.SaveChangesAsync();
            Console.WriteLine("Matched Hash");
            return true;
        }

        return false;
    }
    
}