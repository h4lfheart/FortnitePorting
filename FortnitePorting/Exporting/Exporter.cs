using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Rig;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Types;
using FortnitePorting.Extensions;
using FortnitePorting.Models;
using FortnitePorting.Models.API;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Services;
using FortnitePorting.Views;
using Newtonsoft.Json;
using Serilog;
using BaseAssetInfo = FortnitePorting.Models.Assets.Base.BaseAssetInfo;

namespace FortnitePorting.Exporting;

public static class Exporter
{
    public static async Task ExportTastyRig(ExportDataMeta metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            var serverType = metaData.ExportLocation.ServerType;
            if (serverType is EExportServerType.None)
                return;
           
            if (!await ExportClient.IsRunning(serverType))
            {
                var serverName = serverType.Description;
                Info.Message($"{serverName} Server", $"The {serverName} Plugin for Fortnite Porting is not currently installed or running.", 
                    severity: InfoBarSeverity.Error, closeTime: 3.0f,
                    useButton: true, buttonTitle: "Install Plugin", buttonCommand: () =>
                    {
                        Navigation.App.Open<PluginView>();
                        Navigation.Plugin.Open(metaData.ExportLocation);
                    });
                return;
            }
            
            var exportData = new ExportData
            {
                MetaData = metaData,
                Exports = [new TastyExport(metaData)]
            };
        
            await ExportClient.SendExportAsync(serverType, exportData);
        });
    }
    
    public static async Task<bool> Export(Func<IEnumerable<BaseExport>> exportFunction, ExportDataMeta metaData)
    {
        if (metaData.ExportLocation is EExportLocation.CustomFolder && await App.BrowseFolderDialog() is { } path)
        {
            metaData.CustomPath = path;
        }

        var exportedProperly = false;
        await TaskService.RunAsync(async () =>
        {
            var serverType = metaData.ExportLocation.ServerType;
            if (serverType is EExportServerType.None)
            {
                var exports = exportFunction.Invoke();
                foreach (var export in exports) await export.WaitForExports();
            }
            else
            {
                if (!await ExportClient.IsRunning(serverType))
                {
                    var serverName = serverType.Description;
                    Info.Message($"{serverName} Server", $"The {serverName} Plugin for Fortnite Porting is not currently installed or running.", 
                        severity: InfoBarSeverity.Error, closeTime: 3.0f,
                        useButton: true, buttonTitle: "Install Plugin", buttonCommand: () =>
                        {
                            Navigation.App.Open<PluginView>();
                            Navigation.Plugin.Open(metaData.ExportLocation);
                        });
                    return;
                }

                var exports = exportFunction().ToArray();
                foreach (var export in exports) await export.WaitForExports();
            
                var exportData = new ExportData
                {
                    MetaData = metaData,
                    Exports = exports
                };
            
                await ExportClient.SendExportAsync(serverType, exportData);
            }

            exportedProperly = true;
        });

        return exportedProperly;
    }
    
    public static async Task<bool> Export(IEnumerable<BaseAssetInfo> assets, ExportDataMeta metaData)
    {
        return await Export(() => assets.Select(baseAssetInfo =>
        {
            if (baseAssetInfo is AssetInfo assetInfo)
            {
                var asset = assetInfo.Asset;
                var styles = metaData.ExportLocation.IsFolder ? assetInfo.GetAllStyles() : assetInfo.GetSelectedStyles();
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
    
    public static async Task<bool> Export(List<KeyValuePair<UObject, EExportType>> assets, ExportDataMeta metaData)
    {
        return await Export(() => assets.Select(kvp => CreateExport(kvp.Key.Name, kvp.Key, kvp.Value, [], metaData)), metaData);
    }
    
    public static async Task<bool>? Export(IEnumerable<UObject> assets, EExportType type, ExportDataMeta metaData)
    {
        return await Export(() => assets.Select(asset => CreateExport(asset.Name, asset, type, [], metaData)), metaData);
    }
    
    public static async Task<bool> Export(UObject asset, EExportType type, ExportDataMeta metaData)
    {
        return await Export(() => [CreateExport(asset.Outer?.Name.SubstringAfterLast("/") ?? asset.Name, asset, type, [], metaData)], metaData);
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
            UAnimMontage => EExportType.Animation,
            UAnimSequenceBase => EExportType.Animation,
            UFontFace => EExportType.Font,
            UPoseAsset => EExportType.PoseAsset,
            UDNAAsset => EExportType.PoseAsset,
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
            var assetLoaders = AssetLoading.Categories
                .SelectMany(category => category.Loaders)
                .ToArray();

            foreach (var loader in assetLoaders)
            {
                if (loader.ClassNames.Contains(asset.ExportType))
                {
                    exportType = loader.Type;
                    break;
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
    
    private static BaseExport CreateExport(string displayName, UObject asset, EExportType exportType, BaseStyleData[] styles, ExportDataMeta metaData)
    {
        var path = asset.GetPathName();
        Info.Message($"Exporting", asset.Name, id: path, autoClose: false);

        ExportProgressUpdate updateDelegate = (name, current, total) =>
        {
            var title = $"{displayName} - {current} / {total}";
            var message = $"{name}";
            Info.UpdateTitle(id: path, title);
            Info.UpdateMessage(id: path, message);
            Log.Information("{Title}: {Message}", title, message);
        };

        metaData.UpdateProgress += updateDelegate;
        
        var primitiveType = exportType.GetPrimitiveType();
        BaseExport export = primitiveType switch
        {
            EPrimitiveExportType.Mesh => new MeshExport(displayName, asset, styles, exportType, metaData),
            EPrimitiveExportType.Texture => new TextureExport(displayName, asset, exportType, metaData),
            EPrimitiveExportType.Sound => new SoundExport(displayName, asset, exportType, metaData),
            EPrimitiveExportType.Animation => new AnimExport(displayName, asset, styles, exportType, metaData),
            EPrimitiveExportType.Font => new FontExport(displayName, asset, exportType, metaData),
            EPrimitiveExportType.PoseAsset => new PoseAssetExport(displayName, asset, exportType, metaData),
            EPrimitiveExportType.Material => new MaterialExport(displayName, asset, exportType, metaData),
            _ => throw new NotImplementedException($"Exporting {primitiveType} assets is not supported yet.")
        };
        
        Info.CloseMessage(id: path);
        metaData.UpdateProgress -= updateDelegate;

        return export;
    }
}