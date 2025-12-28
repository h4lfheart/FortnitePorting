using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardLiveExport(string objectPath) : ObservableObject
{
    [ObservableProperty] private string _objectPath = objectPath;
    
    [ObservableProperty] private string _objectName;
    [ObservableProperty] private string _category;
    [ObservableProperty] private Bitmap _exportBitmap;
    [ObservableProperty] private Dictionary<Guid, int> _contributions;
    
    public string ID => ObjectPath.SubstringAfterLast("/").SubstringBefore(".");
    
    private static Dictionary<string, Bitmap> CachedBitmaps = [];
    private static Dictionary<string, UObject> CachedObjects = [];

    public async Task<bool> Load()
    {
        var assetLoaders = AssetLoading.Categories
            .SelectMany(category => category.Loaders)
            .ToArray();
        
        if (ObjectPath.StartsWith("Custom/") )
        {
            ObjectName = ObjectPath.SubstringAfter("/");
            
            var customAssets = assetLoaders.SelectMany(loader => loader.CustomAssets);
            var customAsset = customAssets.FirstOrDefault(asset => asset.Name.Equals(ObjectName));
            if (customAsset is null) return false;
            
            ExportBitmap = customAsset.IconBitmap.ToWriteableBitmap();
            CachedBitmaps[ObjectPath] = ExportBitmap;
            
            return true;
        }
        
        if (!UEParse.FinishedLoading)
        {
            SetFailureDefaults();
            return false;
        }

        if (!CachedObjects.TryGetValue(ObjectPath, out var asset))
        {
            asset = await UEParse.Provider.SafeLoadPackageObjectAsync(ObjectPath);
        }
        
        if (asset is null) 
        {
            SetFailureDefaults();
            return false;
        }
        
        var assetLoader = assetLoaders.FirstOrDefault(loader => loader.ClassNames.Contains(asset.ExportType));
        if (assetLoader is null)
        {
            ObjectName = ID;
            ExportBitmap = asset.GetEditorIconBitmap();
            return true;
        }
        
        if (CachedBitmaps.TryGetValue(ObjectPath, out var existingBitmap))
        {
            ExportBitmap = existingBitmap;
        }
        else
        {
            ExportBitmap = assetLoader.IconHandler(asset)?.Decode()?.ToWriteableBitmap() ?? asset.GetEditorIconBitmap();
            CachedBitmaps[ObjectPath] = ExportBitmap;
        }
        
        ObjectName = assetLoader.DisplayNameHandler(asset) ?? ID;


        return true;
    }
    

    private void SetFailureDefaults()
    {
        ExportBitmap = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/DataAsset_64x.png");
        ObjectName = ID;
    }
}