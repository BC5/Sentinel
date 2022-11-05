using System.Text.Json;
using System.Text.Json.Serialization;
using ImageMagick;

namespace Sentinel.ImageProcessing.Operations;

public interface IImageOperation
{
    public Task<Content> Execute(Content input);
    public Task<MagickImage> AsyncExecute(MagickImage input);
    public MagickImage Execute(MagickImage input);

    public MagickImageCollection ExecuteGif(MagickImageCollection input);

    public Task<MagickImageCollection> AsyncExecuteGif(MagickImageCollection input);

    public void PassArgument(string arg);
    

}