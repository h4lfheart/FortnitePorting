namespace FortnitePorting.Models;

public class FortniteWrappedItem
{
    public EAssetType Type;
    public int Count;

    public FortniteWrappedItem(EAssetType type)
    {
        Type = type;
    }
}