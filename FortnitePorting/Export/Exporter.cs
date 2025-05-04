using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Animations;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;

using FortnitePorting.Export.Models;
using FortnitePorting.Export.Types;
using FortnitePorting.Extensions;
using FortnitePorting.Models;
using FortnitePorting.Models.API;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using BaseAssetInfo = FortnitePorting.Models.Assets.Base.BaseAssetInfo;

namespace FortnitePorting.Export;

public static class Exporter
{
    public static async Task Export(Func<IEnumerable<BaseExport>> exportFunction, ExportDataMeta metaData)
    {
        if (metaData.ExportLocation is EExportLocation.CustomFolder && await App.BrowseFolderDialog() is { } path)
        {
            metaData.CustomPath = path;
        }
        
        await TaskService.RunAsync(async () =>
        {
            var serverType = metaData.ExportLocation.ServerType();
            if (serverType is EExportServerType.None)
            {
                var exports = exportFunction.Invoke();
                foreach (var export in exports) export.WaitForExports();
            }
            else
            {
                if (await Api.FortnitePortingServer.PingAsync(serverType) is false)
                {
                    var serverName = serverType.GetDescription();
                    Info.Message($"{serverName} Server", $"The {serverName} Plugin for Fortnite Porting is not currently installed, running, or is busy.", 
                        severity: InfoBarSeverity.Error, false,
                        useButton: true, buttonTitle: "Install Plugin", buttonCommand: () =>
                        {
                            Navigation.App.Open<PluginView>();
                            Navigation.Plugin.Open(metaData.ExportLocation);
                        });
                    return;
                }

                var exports = exportFunction().ToArray();
                foreach (var export in exports) export.WaitForExports();
            
                var exportData = new ExportData
                {
                    MetaData = metaData,
                    Exports = exports
                };
            
                var data = JsonConvert.SerializeObject(exportData);
                await Api.FortnitePortingServer.SendAsync(data, serverType);
            }
        });
    }
    
    public static async Task Export(IEnumerable<BaseAssetInfo> assets, ExportDataMeta metaData)
    {
        await Export(() => assets.Select(baseAssetInfo =>
        {
            if (baseAssetInfo is AssetInfo assetInfo)
            {
                var asset = assetInfo.Asset;
                var styles = metaData.ExportLocation.IsFolder() ? assetInfo.GetAllStyles() : assetInfo.GetSelectedStyles();
                var exportType = asset.CreationData.ExportType;

                return CreateExport(asset.CreationData.DisplayName, asset.CreationData.Object, exportType, styles,
                    metaData);
            }

            if (baseAssetInfo is CustomAssetInfo customAssetInfo)
            {
                return new MeshExport(customAssetInfo.Asset.Asset, customAssetInfo.Asset.CreationData.ExportType, metaData);
            }

            return null;
        })!, metaData);
    }
    
    public static async Task Export(List<KeyValuePair<UObject, EExportType>> assets, ExportDataMeta metaData)
    {
        await Export(() => assets.Select(kvp => CreateExport(kvp.Key.Name, kvp.Key, kvp.Value, [], metaData)), metaData);
    }
    
    public static async Task Export(IEnumerable<UObject> assets, EExportType type, ExportDataMeta metaData)
    {
        await Export(() => assets.Select(asset => CreateExport(asset.Name, asset, type, [], metaData)), metaData);
    }
    
    public static async Task Export(UObject asset, EExportType type, ExportDataMeta metaData)
    {
        await Export(() => [CreateExport(asset.Name, asset, type, [], metaData)], metaData);
    }

    public static EExportType DetermineExportType(UObject asset) 
    {
        var exportType = asset switch
        {
            USkeletalMesh => EExportType.Mesh,
            UStaticMesh => EExportType.Mesh,
            USkeleton => EExportType.Mesh,
            UBlueprintGeneratedClass => EExportType.Mesh,
            UWorld => EExportType.World,
            UTexture => EExportType.Texture,
            UVirtualTextureBuilder => EExportType.Texture,
            UBuildingTextureData => EExportType.Texture,
            USoundWave => EExportType.Sound,
            USoundCue => EExportType.Sound,
            UAnimSequence => EExportType.Animation,
            UAnimMontage => EExportType.Animation,
            UFontFace => EExportType.Font,
            UPoseAsset => EExportType.PoseAsset,
            UMaterialInstance => EExportType.MaterialInstance,
            UMaterial => EExportType.Material,
            _ => EExportType.None
        };

        if (exportType is EExportType.None)
        {
            exportType = asset.ExportType switch
            {
                "CustomCharacterPart" => EExportType.CharacterPart,
                _ => EExportType.None
            };
        }

        if (exportType is EExportType.None)
        {
            // TODO do the dependency injection and make an exporter service
            var assetLoaderService = AppServices.Services.GetRequiredService<AssetLoaderService>();
            var assetLoaders = assetLoaderService.Categories
                .SelectMany(category => category.Loaders)
                .ToArray();

            foreach (var loader in assetLoaders)
            {
                if (loader.ClassNames.Contains(asset.ExportType))
                {
                    exportType = loader.Type;
                }
            }
        }

        return exportType;
    }
    
    public static string FixPath(string path)
    {
        var outPath = path.SubstringBeforeLast(".");
        var extension = path.SubstringAfterLast(".");
        if (extension.Equals("umap"))
        {
            if (outPath.Contains("_Generated_"))
            {
                outPath += "." + path.SubstringBeforeLast("/_Generated").SubstringAfterLast("/");
            }
        }

        return outPath;
    }
    
    private static BaseExport CreateExport(string name, UObject asset, EExportType exportType, BaseStyleData[] styles, ExportDataMeta metaData)
    {
        var path = asset.GetPathName();
        Info.Message($"Exporting {name}", $"Exporting: {asset.Name}", id: path, autoClose: false);

        ExportProgressUpdate updateDelegate = (name, current, total) =>
        {
            var message = $"{current} / {total} \"{name}\"";
            Info.UpdateMessage(id: path, message: message);
            Log.Information(message);
        };

        metaData.UpdateProgress += updateDelegate;
        
        var primitiveType = exportType.GetPrimitiveType();
        BaseExport export = primitiveType switch
        {
            EPrimitiveExportType.Mesh => new MeshExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.Texture => new TextureExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.Sound => new SoundExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.Animation => new AnimExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.Font => new FontExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.PoseAsset => new PoseAssetExport(name, asset, styles, exportType, metaData),
            EPrimitiveExportType.Material => new MaterialExport(name, asset, styles, exportType, metaData),
            _ => throw new NotImplementedException($"Exporting {primitiveType} assets is not supported yet.")
        };
        
        Info.CloseMessage(id: path);
        metaData.UpdateProgress -= updateDelegate;

        GC.Collect();
        return export;
    }
}