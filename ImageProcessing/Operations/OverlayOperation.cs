using ImageMagick;

namespace Sentinel.ImageProcessing.Operations;

public class OverlayOperation : ImageOperation
{
    public string SecondaryAsset { get; set; } = "operations:scanlines";
    public bool Crop { get; set; } = true;
    public double Scale { get; set; } = 1;
    public Gravity SecondaryGravity { get; set; } = Gravity.Center;
    
    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        string path = Assets.GetImagePath(SecondaryAsset);
        MagickImage input2 = new MagickImage(path);
        
        if (Scale != 1)
        {
            int x = input2.Width;
            int y = input2.Height;
            
            input2.Resize((int)(x*Scale),(int)(y*Scale));
        }
        
        if (Crop)
        {
            input2.Crop(input.Width,input.Height);
        }

        MagickImageCollection overlay = new();
        overlay.Add(input);
        overlay.Add(input2);
        return (MagickImage) overlay.Flatten();
    }

    public override async Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input)
    {
        throw new NotImplementedException();
    }

    public override void PassArgument(string arg)
    {
        throw new NotImplementedException();
    }
}