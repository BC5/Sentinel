using NTextCat;

namespace Sentinel;

public class TextCat
{
    private RankedLanguageIdentifier _identifier;
    
    public TextCat(string model)
    {
        var f = new RankedLanguageIdentifierFactory();
        _identifier = f.Load(model);
    }

    public bool IsFrench(string str)
    {
        if (str == "hon hon hon") return true;
        
        var lang = _identifier.Identify(str).FirstOrDefault();
        if (lang != null && lang.Item1.Iso639_3 == "fra") return true;
        return false;
    }
}