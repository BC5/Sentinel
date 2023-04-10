using Discord.Interactions;
using ImageMagick;
using Newtonsoft.Json;

namespace Sentinel.Bot.ImageProcessing.Operations;

public class OperationCommand : InteractionModuleBase
{
    private AssetManager _assets;

    public OperationCommand(AssetManager am)
    {
        _assets = am;
    }

    [SlashCommand("task", "executes a predefined list of instructions")]
    public async Task ExecuteTask(string task, string args)
    {
        try
        {
            await DeferAsync();
            string jsontext = File.ReadAllText(_assets.GetJsonPath("operations", task));
            var opt = new JsonSerializerSettings();
            opt.TypeNameHandling = TypeNameHandling.Auto;
            OperationSequence? sequence = JsonConvert.DeserializeObject<OperationSequence>(jsontext, opt);

            if (sequence == null)
            {
                await FollowupAsync("Error reading JSON", ephemeral: true);
                return;
            }

            string[] argss = args.Split(";");

            object? img = sequence.Execute(_assets, argss);

            if (img == null)
            {
                await FollowupAsync("Error", ephemeral: true);
            }
            else
            {
                if (img is MagickImageCollection)
                {
                    MagickImageCollection gif = (MagickImageCollection)img;
                    var ms = new MemoryStream(gif.ToByteArray());
                    await Context.Interaction.FollowupWithFileAsync(ms, "operation.gif");
                }
                else if (img is MagickImage)
                {
                    MagickImage png = (MagickImage)img;
                    var ms = new MemoryStream(png.ToByteArray());
                    await Context.Interaction.FollowupWithFileAsync(ms, "operation.png");
                }
                else
                {
                    await FollowupAsync("Error", ephemeral: true);
                    Console.WriteLine("Unexpected type " + img.GetType());
                }


            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await FollowupAsync("Oopsie! " + e.GetType().Name);
        }
    }

    [SlashCommand("operation", "executes an operation on an image")]
    public async Task ExecuteOperation(string operation, string? args = null)
    {
        ImageOperation? op = null;
        switch (operation.ToLower())
        {
            case "aberration":
                op = new AberrationOperation();
                break;
            case "caption":
                op = new CaptionOperation();
                break;
            case "explode":
                op = new ExplodeOperation();
                break;
            case "noise":
                op = new NoiseOperation();
                break;
            case "scanlines":
                var op1 = new OverlayOperation();
                op1.SecondaryAsset = "operations:scanlines";
                op1.Scale = 2;
                op = op1;
                break;
            case "vcr":
                op = new TextOperation
                {
                    TextColor = "#FFFFFFFF",
                    BackgroundColour = "#FF000000",
                    FontFamily = "VCR OSD Mono",
                    FontSize = 60,
                };
                break;
            default:
                await RespondAsync("Invalid Operation");
                return;
        }
        await DeferAsync();
        op.SetAM(_assets);
        try
        {
            if (args != null)
            {
                op.PassArgument(args);
            }
            Content? content = await _assets.GetContent(Context.User.Id, Context.Channel);
            if (content == null)
            {
                await FollowupAsync("Couldn't find an image to operate on");
                return;
            }

            MagickImage img = await op.AsyncExecute(new MagickImage(content.Data));
            MemoryStream ms = new(img.ToByteArray(MagickFormat.Png00));
            await FollowupWithFileAsync(ms, "operation.png");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

}