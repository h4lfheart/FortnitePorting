using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Types;

public class TextureExport : BaseExport
{
    public List<string> Textures = [];
    
    private static readonly Dictionary<EExportType, string> TextureNames = new()
    {
        { EExportType.Spray, "DecalTexture" },
        { EExportType.Banner, "LargePreviewImage" },
        { EExportType.LoadingScreen, "BackgroundImage" },
        { EExportType.Emoticon, "SpriteSheet" }
    };
    
    public TextureExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        var textures = new List<UTexture>();
        switch (asset)
        {
            case UVirtualTextureBuilder virtualTextureBuilder:
            {
                textures.AddIfNotNull(virtualTextureBuilder.Texture.Load<UVirtualTexture2D>());
                break;
            }
            case UTexture texture:
            {
                textures.Add(texture);
                break;
            }
            case UBuildingTextureData textureData:
            {
                textures.AddIfNotNull(textureData.Diffuse);
                textures.AddIfNotNull(textureData.Normal);
                textures.AddIfNotNull(textureData.Specular);
                break;
            }
            default:
            {
                textures.AddIfNotNull(asset.GetOrDefault<UTexture2D?>(TextureNames[exportType]) ?? asset.GetDataListItem<UTexture2D>("LargeIcon", "Icon"));
                break;
            }
        }

        foreach (var texture in textures)
        {
            if (metaData.ExportLocation.IsFolder)
            {
                Exporter.Export(texture, returnRealPath: true, synchronousExport: true);
            }
            else
            {
                Textures.Add(Exporter.Export(texture));
            }
        }
       
    }
    
}