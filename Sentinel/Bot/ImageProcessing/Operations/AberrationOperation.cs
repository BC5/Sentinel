using ImageMagick;

namespace Sentinel.Bot.ImageProcessing.Operations;

public class AberrationOperation : ImageOperation
{
    public int[] RedNudge { get; set; } = { 2, 1 };
    public int[] GreenNudge { get; set; } = { 2, 1 };

    public override async Task<MagickImage> AsyncExecute(MagickImage input)
    {
        return await Task.Run(() => Aberrate(input, RedNudge[0], RedNudge[1], GreenNudge[0], GreenNudge[1]));
    }

    public override async Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input)
    {
        throw new NotImplementedException();
    }

    private static MagickImage Aberrate(MagickImage input, int xR = 2, int yR = 1, int xG = 2, int yG = 1)
    {
        var channels = input.Separate(Channels.RGB);
        var reassembled = new MagickImageCollection();

        int i = 0;
        foreach (var image in channels)
        {
            //Nudge Red Channel
            if (i == 0)
            {
                image.Extent(input.Width + xR, input.Height + yR, Gravity.Northeast, MagickColors.Black);
                image.Crop(input.Width, input.Height, Gravity.Southwest);
            }

            //Nudge Green Channel
            if (i == 1)
            {
                image.Extent(input.Width + xG, input.Height + yG, Gravity.Southwest, MagickColors.Black);
                image.Crop(input.Width, input.Height, Gravity.Northeast);
            }
            reassembled.Add(image);
            i++;
        }
        return (MagickImage)reassembled.Combine(ColorSpace.RGB);

    }

    public override void PassArgument(string arg)
    {
        return;
    }
}