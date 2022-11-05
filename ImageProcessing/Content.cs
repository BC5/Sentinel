using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using VideoLibrary;

namespace Sentinel.ImageProcessing;

public class Content
{
    public byte[] Data { get; }
    public ContentType Type { get; }
    public FileType FType { get; }

    public Content(byte[] data, ContentType contentType, FileType fileType)
    {
        this.Data = data;
        this.Type = contentType;
        this.FType = fileType;
    }

    //https://g.tenor.com/v1/gifs?ids=21657535&media_filer=minimal&limit=1&key=MYL3DJEAM6W4
    public static async Task<string> FromTenor(string url, string tenorkey)
    {
        HttpClient http = new();
        TenorResponse? tenor = await http.GetFromJsonAsync<TenorResponse>
            ($"https://g.tenor.com/v1/gifs?ids={url.Split('-').Last()}&media_filer=minimal&limit=1&key={tenorkey}");
        return tenor.results[0].media[0].gif.url;
    }

    public static async Task<Content?> FromYoutube(string url)
    {
        var yt = YouTube.Default;
        var video = await yt.GetVideoAsync(url);
        byte[] bytes = await video.GetBytesAsync();
        return new Content(bytes, ContentType.Video, FileType.MP4);
    }
    
    public static async Task<Content?> FromUrl(string url)
    {
        Uri uri = new Uri(url);
        switch (uri.Host)
        {
            case "tenor.com":
                //TODO: Load tenor key from config
                url = await FromTenor(url,"MYL3DJEAM6W4");
                break;
            case "www.youtube.com":
            case "youtube.com":
            case "youtu.be":
                return await FromYoutube(url);
            default:
                break;
        }
        
        HttpClient http = new();
        HttpResponseMessage resp = await http.GetAsync(url);

        if (resp.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine(resp.StatusCode.ToString());
            return null;
        }

        byte[] content = await resp.Content.ReadAsByteArrayAsync();
        ContentType t = ContentType.Unknown;
        FileType ft = FileType.Unknown;
        
        if (resp.Content.Headers.ContentType != null) switch (resp.Content.Headers.ContentType.MediaType)
        {
            case "image/png":
                t = ContentType.StaticImage;
                ft = FileType.PNG;
                break;
            case "image/gif":
                t = ContentType.UnknownGIF;
                ft = FileType.GIF;
                break;
            default:
                Console.WriteLine("unknown mime type: " + resp.Content.Headers.ContentType.MediaType);
                Console.WriteLine("will assume image by default");
                t = ContentType.StaticImage;
                ft = FileType.Unknown;
                break;
        }
        else
        {
            Console.WriteLine("null mime type");
            Console.WriteLine("will assume image by default");
            t = ContentType.StaticImage;
            ft = FileType.Unknown;
        }

        if (t == ContentType.UnknownGIF)
        {
            DateTime start = DateTime.Now;
            using (Bitmap b = new Bitmap(new MemoryStream(content)))
            {
                var dim = new FrameDimension(b.FrameDimensionsList[0]);
                if (b.GetFrameCount(dim) == 1)
                {
                    t = ContentType.StaticImage;
                }
                else
                {
                    t = ContentType.AnimatedImage;
                }
            }
            DateTime finish = DateTime.Now;
            Console.WriteLine($"Detected GIF animation in {(finish-start).TotalMilliseconds}ms");
        }

        return new Content(content, t, ft);
    }

    public enum FileType
    {
        GIF,
        PNG,
        MP3,
        MP4,
        Unknown
    }
    
    public enum ContentType
    {
        StaticImage,
        AnimatedImage,
        UnknownGIF,
        Video,
        Audio,
        Unknown
    }
    
}