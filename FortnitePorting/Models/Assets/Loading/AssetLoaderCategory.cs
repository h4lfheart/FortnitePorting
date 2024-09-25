using System.Collections.Generic;
using FortnitePorting.Shared;

namespace FortnitePorting.Models.Assets.Loading;

public class AssetLoaderCategory(EAssetCategory category)
{
    public readonly EAssetCategory Category = category;
    
    public List<AssetLoader> Loaders = [];
}