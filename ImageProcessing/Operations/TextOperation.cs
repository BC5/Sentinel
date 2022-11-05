using System.Drawing;
using ImageMagick;
using Newtonsoft.Json;
using SkiaSharp;
using Topten.RichTextKit;
using TextAlignment = Topten.RichTextKit.TextAlignment;

namespace Sentinel.ImageProcessing.Operations;

public class TextOperation : ImageOperation
{
    public double FontSize { get; set; } = 0;
    public int[] TextBounds { get; set; } = {0, 0, 0, 0};
    public Gravity TextGravity { get; set; } = Gravity.Center;
    public string TextContent { get; set; } = "no input";
    public string FontFamily { get; set; } = "Arial";
    public string BackgroundColour { get; set; } = "#00000000";
    public string TextColor { get; set; } = "#FFFFFFFF";
    public double TextRotation { get; set; } = 0;

    public bool AllFrames { get; set; } = true;
    public int FirstFrame { get; set; } = 0;
    public int LastFrame { get; set; } = 0;

    public int FontWeight { get; set; } = 400;
    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        if(TextBounds.SequenceEqual(new []{0,0,0,0})) GuessBounds(input);
        byte[] textpng = await Task.Run(() => GenerateText(TextContent, TextBounds[2]-TextBounds[0], TextBounds[3]-TextBounds[1], FontFamily, FontWeight, (int) FontSize, 5, 5,(TextGravity == Gravity.Center), TextColor,BackgroundColour));
        MagickImage text = new(textpng);
        if(TextRotation != 0) text.Rotate(TextRotation);
        input.Composite(text,TextBounds[0],TextBounds[1],CompositeOperator.Over);
        return input;
    }

    public override async Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input)
    {
        if(TextBounds.SequenceEqual(new []{0,0,0,0})) GuessBounds(input[0]);
        byte[] textpng = await Task.Run(() => GenerateText(TextContent, TextBounds[2]-TextBounds[0], TextBounds[3]-TextBounds[1], FontFamily, FontWeight,(int) FontSize, 5, 5,(TextGravity == Gravity.Center), TextColor,BackgroundColour));
        MagickImage text = new(textpng);
        if(TextRotation != 0) text.Rotate(TextRotation);

        List<Task> tasks = new List<Task>();

        if (AllFrames)
        {
            FirstFrame = 0;
            LastFrame = int.MaxValue;
        }

        for(int i = FirstFrame; i < input.Count && i < LastFrame; i++)
        {
            var i1 = i;
            tasks.Add(Task.Run(() => {input[i1].Composite(text,TextBounds[0],TextBounds[1],CompositeOperator.Over);}));
        }
        await Task.WhenAll(tasks);
        return input;
    }

    private void GuessBounds(IMagickImage img)
    {
        if (TextGravity == Gravity.Center)
        {
            int[] center = new int[] {img.Width / 2, img.Height / 2};
            int w = (img.Width / 4) / 2;
            int h = (img.Height / 8) / 2;
            TextBounds = new int[] {center[0]-w,center[1]-h,center[0]+w,center[1]+h};
        }
        else
        {
            TextBounds = new int[] {0,0,img.Width,img.Height/7};
        }

        Console.Write("TB:");
        foreach (var i in TextBounds)
        {
            Console.Write(i + " ");
        }
        Console.Write("\n");
    }
    
    public override void PassArgument(string arg)
    {
        TextContent = arg;
    }

    public static byte[] GenerateText(string text, int w, int h, string font = "Arial", int weight = 400, int maxSize = 100, int minSize = 10, int decrement = 5, bool center = true, string ForegroundColour = "#000000FF", string BackgroundColour = "#00000000")
    {
        TextBlock tb = new TextBlock();
        
        tb.MaxWidth = w;
        if(center) tb.Alignment = TextAlignment.Center;

        Style tbstyle = new Style()
        {
            FontSize = maxSize,
            FontFamily = font,
            TextColor = SKColor.Parse(ForegroundColour),
            FontWeight = weight
        };
        
        tb.AddText(text,tbstyle);

        int size = maxSize;
        for (; size > minSize; size = size - decrement)
        {
            tbstyle.FontSize = size;
            tb.ApplyStyle(0,tb.Length,tbstyle);
            if (tb.MeasuredHeight <= h) break;
        }

        float spareHeight = h - tb.MeasuredHeight;
        if (spareHeight < 0 || !center) spareHeight = 0;
        
        SKSurface surface = SKSurface.Create(new SKImageInfo(w,h));
        var bg = SKColor.Parse(BackgroundColour);
        surface.Canvas.Clear(bg);
        tb.Paint(surface.Canvas,new SKPoint(0,spareHeight/2));
        var png = surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100);
        
        return png.ToArray();
    }
}