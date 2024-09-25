using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.ViewModels.Settings;
using Serilog;

namespace FortnitePorting.Export.Types;

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
            if (metaData.ExportLocation.IsFolder())
            {
                var exportPath = Exporter.Export(texture, returnRealPath: true, synchronousExport: true);
                Launch(System.IO.Path.GetDirectoryName(exportPath)!);
            }
            else
            {
                Textures.Add(Exporter.Export(texture));
            }
        }
       
    }
    
}