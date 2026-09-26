using System.Collections.Generic;

namespace FortnitePorting.Models.Assets.Loading;

public class AssetLoaderCategory(EAssetCategory category)
{
    public readonly EAssetCategory Category = category;
    
    public List<AssetLoader> Loaders = [];
}