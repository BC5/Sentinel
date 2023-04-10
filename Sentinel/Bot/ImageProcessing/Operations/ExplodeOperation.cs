using ImageMagick;

namespace Sentinel.Bot.ImageProcessing.Operations;

public class ExplodeOperation : ImageOperation
{

    public double ExplosionAmount { get; set; } = 1;

    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        input.Implode(0 - ExplosionAmount, PixelInterpolateMethod.Bilinear);
        return input;
    }

    public override async Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input)
    {
        throw new NotImplementedException();
    }

    public override void PassArgument(string arg)
    {
        ExplosionAmount = int.Parse(arg);
    }
}