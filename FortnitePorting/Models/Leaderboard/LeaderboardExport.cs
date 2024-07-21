using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;
using OpenTK.Graphics.OpenGL;

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
    
    public string ID => ObjectPath.SubstringAfterLast("/").SubstringBefore(".");
    
    private static Dictionary<string, Bitmap> CachedBitmaps = [];
    private static Dictionary<string, UObject> CachedObjects = [];

    public async Task Load()
    {
        if (!CUE4ParseVM.FinishedLoading)
        {
            ExportBitmap = LeaderboardVM.GetMedalBitmap(Ranking);
            ObjectName = ID;
            return;
        }

        if (!CachedObjects.TryGetValue(ObjectPath, out var asset))
        {
            asset = await CUE4ParseVM.Provider.LoadObjectAsync(ObjectPath);
        }
        
        var assetLoader =  AssetLoaderCollection.CategoryAccessor.Loaders.FirstOrDefault(loader => loader.ClassNames.Contains(asset.ExportType));
        if (assetLoader is null) 
        {
            ExportBitmap = LeaderboardVM.GetMedalBitmap(Ranking);
            ObjectName = ID;
            return;
        }

        
        ShowMedal = true;
        if (CachedBitmaps.TryGetValue(ObjectPath, out var existingBitmap))
        {
            ExportBitmap = existingBitmap;
        }
        else
        {
            ExportBitmap = assetLoader.IconHandler(asset)?.Decode()?.ToWriteableBitmap() ?? LeaderboardVM.GetMedalBitmap(Ranking);
            CachedBitmaps[ObjectPath] = ExportBitmap;
        }
        
        ObjectName = assetLoader.DisplayNameHandler(asset) ?? ID;

        if (Ranking <= 3)
        {
            MedalBitmap = LeaderboardVM.GetMedalBitmap(Ranking);
        }
    }
}