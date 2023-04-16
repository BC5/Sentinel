using ImageMagick;

namespace Sentinel.ImageProcessing.Operations;

public class OperationSequence
{
    public string Name { get; set; }
    public string? InitialAsset { get; set; }

    public AssetManager.AssetType InitialAssetType { get; set; } = AssetManager.AssetType.Image;
    public Dictionary<int, int> ArgumentAssigment { get; set; } = new();
    public List<string> ArgumentNames { get; set; } = new();
    public List<OperationEntry> Operations { get; set; } = new();
    
    

    public OperationSequence(string name)
    {
        this.Name = name;
    }

    private void PassArguments(string[] args)
    {
        foreach (var entry in ArgumentAssigment)
        {
            ((IImageOperation) Operations[entry.Key].Parameters).PassArgument(args[entry.Value]);
        }
    }

    public object? Execute(AssetManager am, string[] args)
    {
        foreach (OperationEntry op in Operations)
        {
            if (op.Parameters is ImageOperation op2)
            {
                op2.SetAM(am);
            }
        }
        
        if (InitialAsset != null)
        {
            if (InitialAssetType == AssetManager.AssetType.Image)
            {
                MagickImage img = new(am.GetImagePath(InitialAsset));
                return Execute(img, args);
            }
            else
            {
                MagickImageCollection gif = new(am.GetGifPath(InitialAsset));
                return ExecuteGif(gif, args);
            }
        }
        return null;
    }
    
    public MagickImage Execute(MagickImage input, string[] args)
    {
        PassArguments(args);
        foreach (var entry in Operations)
        {
            var operation = (IImageOperation) entry.Parameters;
            input = operation.Execute(input);
        }
        return input;
    }
    
    public MagickImageCollection ExecuteGif(MagickImageCollection input, string[] args)
    {
        PassArguments(args);
        foreach (var entry in Operations)
        {
            var operation = (IImageOperation) entry.Parameters;
            input = operation.ExecuteGif(input);
        }
        return input;
    }
    
    public class OperationEntry
    {
        public string Operation { get; set; }
        public IImageOperation Parameters { get; set; }

        public OperationEntry()
        {
            
        }
        public OperationEntry(IImageOperation operation)
        {
            Operation = operation.GetType().Name;
            Parameters = operation;
        }
    }
}