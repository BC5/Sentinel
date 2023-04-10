using ImageMagick;

namespace Sentinel.Bot.ImageProcessing.Operations;

public class NoiseOperation : ImageOperation
{
    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        input.AddNoise(NoiseType.Gaussian, 0.7, Channels.RGB);
        return input;
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