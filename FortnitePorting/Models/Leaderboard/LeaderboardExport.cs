using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.Utils;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardExport : ObservableObject
{

    [ObservableProperty] private int _ranking;
    [ObservableProperty] private string _objectName;
    [ObservableProperty] private string _objectPath;
    [ObservableProperty] private string _category;
    [ObservableProperty] private int _exportCount;
    [ObservableProperty] private bool _showMedal;

    public string ID => ObjectPath.SubstringAfterLast("/").SubstringBefore(".");
    
    public Bitmap? MedalBitmap => Ranking <= 3 ? LeaderboardVM.GetMedalBitmap(Ranking) : null;
    
    public Task<Bitmap> ExportBitmap => GetBitmap();

    private static Dictionary<string, Bitmap> CachedBitmaps = [];

    public async Task<Bitmap> GetBitmap()
    {
        if (CachedBitmaps.TryGetValue(ObjectPath, out var existingBitmap))
        {
            ShowMedal = true;
            return existingBitmap;
        }
        
        if (!CUE4ParseVM.FinishedLoading) return LeaderboardVM.GetMedalBitmap(Ranking);
        var asset = await CUE4ParseVM.Provider.LoadObjectAsync(ObjectPath);
        
        var assetLoaders = AssetLoaderCollection.CategoryAccessor.Categories
            .SelectMany(category => category.Loaders)
            .ToArray();

        var assetLoader = assetLoaders.FirstOrDefault(loader => loader.Type.ToString().Equals(Category));
        assetLoader ??= assetLoaders.FirstOrDefault(loader => loader.ClassNames.Contains(asset.ExportType));
        if (assetLoader is null) return LeaderboardVM.GetMedalBitmap(Ranking);
        
        var icon = assetLoader.IconHandler(asset);
        if (icon is null) return LeaderboardVM.GetMedalBitmap(Ranking);

        var bitmap = icon.Decode()!.ToWriteableBitmap();

        ShowMedal = true;
        CachedBitmaps[ObjectPath] = bitmap;
        return bitmap;
    }
}