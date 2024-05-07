using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared;

namespace FortnitePorting.Models.Assets;

public class AssetLoaderCategory(EAssetCategory category)
{
    public readonly EAssetCategory Category = category;
    
    public List<AssetLoader> Loaders = [];
}