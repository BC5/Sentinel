using Discord;
using FFMpegCore;
using ImageMagick;
using VideoLibrary;

namespace Sentinel.Bot.ImageProcessing;

public class AssetManager
{
    string _assetDirectory;
    private string _tempDirectory;
    private Dictionary<ulong, Content> _selections;

    public async Task<Content?> GetContent(ulong user, IMessageChannel channel)
    {
        if (_selections.ContainsKey(user))
        {
            var tmp = _selections[user];
            _selections.Remove(user);
            if (tmp != null)
            {
                return tmp;
            }
        }

        string? x = GetLastEmbed(channel).Result;
        if (x == null) return null;
        Content? content = await Content.FromUrl(x);
        return content;
    }
    
    public static async Task<string?> GetLastEmbed(IMessageChannel textChannel)
    {
        var msgs = textChannel.GetMessagesAsync(5);
        await foreach (var msg in msgs)
        {
            foreach (var m in msg)
            {
                if (m.Embeds.Count > 0)
                {
                    if(ValidEmbed(m.Embeds.First().Type)) return m.Embeds.First().Url;
                }

                if (m.Attachments.Count > 0)
                {
                    if(ValidFile(m.Attachments.First().Filename)) return m.Attachments.First().Url;
                }
            }
        }

        return null;
    }
    
    public static bool ValidFile(string name)
    {
        if (name.EndsWith(".png")) return true;

        return false;
    }
    
    public static bool ValidEmbed(EmbedType et)
    {
        switch (et)
        {
            case EmbedType.Image:
            case EmbedType.Gifv:
                return true;
            default:
                return false;
        }
    }

    public AssetManager(string dir, string tmpdir)
    {
        _assetDirectory = dir;
        _tempDirectory = tmpdir;
        _selections = new Dictionary<ulong, Content>();
    }

    public string GetTempDir()
    {
        return _tempDirectory;
    }

    public string GetImagePath(string library, string resource)
    {
        return @$"{_assetDirectory}/{library}/{GetTypeDirName(AssetType.Image)}/{resource}.png";
    }
    
    public string GetGifPath(string library, string resource)
    {
        return @$"{_assetDirectory}/{library}/{GetTypeDirName(AssetType.Gif)}/{resource}.gif";
    }
    
    public string GetJsonPath(string library, string resource)
    {
        return @$"{_assetDirectory}/{library}/{GetTypeDirName(AssetType.Json)}/{resource}.json";
    }

    public string GetImagePath(string locator)
    {
        var x = locator.Split(":");
        return GetImagePath(x[0], x[1]);
    }

    public string GetGifPath(string locator)
    {
        var x = locator.Split(":");
        return GetGifPath(x[0], x[1]);
    }

    public string GetSoundPath(string library, string resource)
    {
        return @$"{_assetDirectory}/{library}/{GetTypeDirName(AssetType.Sound)}/{resource}.mp3";
    }

    public Content GetImage(string library, string resource)
    {
        byte[] bytes = File.ReadAllBytes(GetImagePath(library, resource));
        return new Content(bytes, Content.ContentType.StaticImage, Content.FileType.PNG);
    }
    
    public Content GetAudio(string library, string resource)
    {
        byte[] bytes = File.ReadAllBytes(GetSoundPath(library, resource));
        return new Content(bytes, Content.ContentType.Audio, Content.FileType.MP3);
    }

    public string[] GetLibraryContents(string library, AssetType at)
    {
        string[] files = Directory.GetFiles($@"{_assetDirectory}/{library}/{GetTypeDirName(at)}");
        //Removing ".mp3",".png",etc.
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }

    public void ImportAudio(string url, string library, string name)
    {
        string videofile = YoutubeDownload(url);
        FFMpeg.ExtractAudio($@"{_tempDirectory}/{videofile}", GetSoundPath(library, name));
    }
    
    private string YoutubeDownload(string url)
    {
        var yt = YouTube.Default;
        var ytv = yt.GetVideo(url);
        File.WriteAllBytes($@"{_tempDirectory}/{ytv.FullName}",ytv.GetBytes());
        return ytv.FullName;
    }


    public static string GetTypeDirName(AssetType at)
    {
        switch (at)
        {
            case AssetType.Sound:
                return "sounds";
            case AssetType.Image:
                return "images";
            case AssetType.Video:
                return "videos";
            case AssetType.Json:
                return "json";
            case AssetType.Gif:
                return "gifs";
        }

        return "";
    }
    
    public enum AssetType
    {
        Sound,
        Image,
        Video,
        Gif,
        Json
    }
    
}