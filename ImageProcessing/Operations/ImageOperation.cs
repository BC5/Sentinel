using ImageMagick;

namespace Sentinel.ImageProcessing.Operations;

public abstract class ImageOperation : IImageOperation
{

    public AssetManager Assets;

    public void SetAM(AssetManager am)
    {
        this.Assets = am;
    }

    public async Task<Content> Execute(Content input)
    {
        switch (input.Type)
        {
            case Content.ContentType.StaticImage:
                byte[] img = (await AsyncExecute(new MagickImage(input.Data))).ToByteArray();
                return new Content(img, Content.ContentType.StaticImage, Content.FileType.PNG);
            case Content.ContentType.AnimatedImage:
                byte[] gif = (await AsyncExecuteGif(new MagickImageCollection(input.Data))).ToByteArray();
                return new Content(gif, Content.ContentType.AnimatedImage, Content.FileType.GIF);
            default:
                throw new Exception($"Did not expect {input.Type} in ImageOperation");
        }
    }
    
    public MagickImage Execute(MagickImage input)
    {
        return AsyncExecute(input).Result;
    }

    public abstract Task<MagickImage> AsyncExecute(MagickImage input);

    public MagickImageCollection ExecuteGif(MagickImageCollection input)
    {
        return AsyncExecuteGif(input).Result;
    }

    public abstract Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input);

    public abstract void PassArgument(string arg);
}