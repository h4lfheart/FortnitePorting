using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Fortnite;
using Microsoft.Extensions.DependencyInjection;
using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using AssetLoader = Avalonia.Platform.AssetLoader;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardExport : ObservableObject
{
    [ObservableProperty] private int _ranking;
    [ObservableProperty] private string _objectName;
    [ObservableProperty] private string _objectPath;
    [ObservableProperty] private string _category;
    [ObservableProperty] private int _exportCount;
    [ObservableProperty] private Bitmap _exportBitmap;
    [ObservableProperty] private bool _showMedal;
    [ObservableProperty] private Bitmap _medalBitmap;
    [ObservableProperty] private Dictionary<Guid, int> _contributions;
    
    public string ID => ObjectPath.SubstringAfterLast("/").SubstringBefore(".");
    
    private static Dictionary<string, Bitmap> CachedBitmaps = [];
    private static Dictionary<string, UObject> CachedObjects = [];

    // returns if is a valid export
    public async Task<bool> Load()
    {
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
        
        // TODO do the dependency injection and make an exporter service
        var assetLoaderService = AppServices.Services.GetRequiredService<AssetLoaderService>();
        var assetLoaders = assetLoaderService.Categories
            .SelectMany(category => category.Loaders)
            .ToArray();
        var assetLoader = assetLoaders.FirstOrDefault(loader => loader.ClassNames.Contains(asset.ExportType));
        if (assetLoader is null)
        {
            ObjectName = ID;
            ExportBitmap = GetObjectBitmap(asset) ?? LeaderboardVM.GetMedalBitmap(Ranking);
            return true;
        }
        
        ShowMedal = true;
        if (CachedBitmaps.TryGetValue(ObjectPath, out var existingBitmap))
        {
            ExportBitmap = existingBitmap;
        }
        else
        {
            ExportBitmap = assetLoader.IconHandler(asset)?.Decode()?.ToWriteableBitmap() ?? GetObjectBitmap(asset) ?? LeaderboardVM.GetMedalBitmap(Ranking);
            CachedBitmaps[ObjectPath] = ExportBitmap;
        }
        
        ObjectName = assetLoader.DisplayNameHandler(asset) ?? ID;

        if (Ranking <= 3)
        {
            MedalBitmap = LeaderboardVM.GetMedalBitmap(Ranking);
        }

        return true;
    }
    
    

    private Bitmap? GetObjectBitmap(UObject obj)
    {
        var typeName = obj switch
        {
            UBuildingTextureData => "DataAsset",
            _ => obj.GetType().Name[1..]
        };
        
        var filePath = $"avares://FortnitePorting/Assets/Unreal/{typeName}_64x.png";
        if (!AssetLoader.Exists(new Uri(filePath))) return null;
        
        return ImageExtensions.AvaresBitmap(filePath);
    }

    private void SetFailureDefaults()
    {
        ExportBitmap = LeaderboardVM.GetMedalBitmap(Ranking);
        ObjectName = ID;
    }
}