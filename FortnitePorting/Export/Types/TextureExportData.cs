using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;

namespace FortnitePorting.Export.Types;

public class TextureExportData : ExportDataBase
{
    public string Path;
    
    private static readonly Dictionary<EAssetType, string> TextureNames = new()
    {
        { EAssetType.Spray, "DecalTexture" },
        { EAssetType.Banner, "LargePreviewImage" },
        { EAssetType.LoadingScreen, "BackgroundImage" },
        { EAssetType.Emoticon, "SpriteSheet" }
    };

    public TextureExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportTargetType exportType) : base(name, asset, styles, type, EExportType.Texture, exportType)
    {
        var texture = asset as UTexture ?? asset.Get<UTexture2D>(TextureNames[type]);
        if (exportType == EExportTargetType.Folder)
        {
            var exportPath = Exporter.Export(texture, true);
            Launch(System.IO.Path.GetDirectoryName(exportPath)!);
        }
        else
        {
            Path = Exporter.Export(texture);
        }
        
    }
}