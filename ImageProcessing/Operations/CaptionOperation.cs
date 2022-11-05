using ImageMagick;

namespace Sentinel.ImageProcessing.Operations;

public class CaptionOperation : ImageOperation
{
    public string CaptionText { get; set; } = "";
    public CaptionStyle Style { get; set; } = CaptionStyle.DDB; 
    
    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        MagickImage cap;
        switch (Style)
        {
            case CaptionStyle.Esm:
                cap = await Task.Run(() => LegacyEsm(input.Width, CaptionText));
                break;
            case CaptionStyle.DDB:
            default:
                cap = await Task.Run(() => NeoEsm(input.Width, CaptionText));
                break;
        }
        var collection = new MagickImageCollection();
        collection.Add(cap);
        collection.Add(input);
        var output = collection.AppendVertically();
        
        if(output is MagickImage image) return image;
        throw new Exception("Expected MagickImage, Got " + output.GetType());
    }

    public override async Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input)
    {
        MagickImage cap;
        switch (Style)
        {
            case CaptionStyle.Esm:
                cap = await Task.Run(() =>  LegacyEsm(input[0].Width, CaptionText));
                break;
            case CaptionStyle.DDB:
            default:
                cap = await Task.Run(() =>  NeoEsm(input[0].Width, CaptionText));
                break;
        }

        //Trying to parallelise this caused issues
        for(int i = 0; i < input.Count; i++)
        {
            var collection = new MagickImageCollection();
            collection.Add(cap);
            collection.Add(input[i]);
            input[i] = collection.AppendVertically();
        }
        return input;
    }

    public override void PassArgument(string arg)
    {
        CaptionText = arg;
    }

    private static MagickImage NeoEsm(int width, string cap)
    {
        int height = (int) (width / 4.5);
        MagickImage img = new(MagickColors.White, width, height);
        var txt = new TextOperation();
        txt.FontFamily = "Roboto Condensed";
        txt.FontWeight = 700;
        txt.TextColor = "#FF000000";
        txt.FontSize = width / 5;
        txt.TextGravity = Gravity.Center;
        txt.TextBounds = new int[] {0, 0, width-1, height-1};
        txt.TextContent = cap;
        return txt.Execute(img);
    }
    
    /// <summary>
    /// Loose emulation of Esmbot's <b>&amp;caption</b> function
    /// </summary>
    /// <param name="width">image to be captioned's width</param>
    /// <param name="cap">caption text</param>
    /// <returns>A caption image to be appended to the original image</returns>
    private static MagickImage LegacyEsm(int width, string cap)
    {
        MagickReadSettings mrs = new();

        mrs.FontPointsize = width / 13;
        mrs.TextGravity = Gravity.Center;
        mrs.Width = width;

        MagickImage caption = new MagickImage();
        caption.Read($"pango:<span font_family=\"Roboto Condensed\" weight=\"bold\">{cap}</span>", mrs);
        caption.Format = MagickFormat.Png;
        caption.Extent(new MagickGeometry(width,(caption.Height + (width/13))),Gravity.Center);

        return caption;

    }
    
    public enum CaptionStyle
    {
        DDB,
        Esm
    }
    
}