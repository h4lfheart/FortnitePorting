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
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using Serilog;

namespace FortnitePorting.Export.Types;

public class TextureExport : BaseExport
{
    public string Path;
    
    private static readonly Dictionary<EExportType, string> TextureNames = new()
    {
        { EExportType.Spray, "DecalTexture" },
        { EExportType.Banner, "LargePreviewImage" },
        { EExportType.LoadingScreen, "BackgroundImage" },
        { EExportType.Emoticon, "SpriteSheet" }
    };
    
    public TextureExport(string name, UObject asset, FStructFallback[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        var texture = asset switch
        {
            UTexture textureRef => textureRef,
            UVirtualTextureBuilder virtualTextureBuilder => virtualTextureBuilder.Texture.Load<UVirtualTexture2D>(),
            _ => asset.GetOrDefault<UTexture2D?>(TextureNames[exportType]) ?? asset.GetDataListItem<UTexture2D>("LargeIcon", "Icon")
        };
        
        if (texture is null) return;
        
        if (metaData.Settings is FolderSettingsViewModel folderSettings)
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