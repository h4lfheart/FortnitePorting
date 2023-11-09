using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;

namespace FortnitePorting.Export.Types;

public class TextureExportData : ExportDataBase
{
    private static readonly Dictionary<EAssetType, string> TextureNames = new()
    {
        { EAssetType.Spray, "DecalTexture" },
        { EAssetType.Banner, "LargePreviewImage" },
        { EAssetType.LoadingScreen, "BackgroundImage" },
    };
    
    public TextureExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType exportType) : base(name, asset, styles, type, exportType)
    {
        var texture = asset.Get<UTexture2D>(TextureNames[type]);
        var exportPath = Exporter.Export(texture, waitForFinish: true);
        AppVM.Launch(Path.GetDirectoryName(exportPath)!);
    }
}