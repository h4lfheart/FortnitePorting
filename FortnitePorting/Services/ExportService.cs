using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
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
using FortnitePorting.CUE4Parse.Models.Unreal.VirtualTexture;
using FortnitePorting.Exporting;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files;
using FortnitePorting.Exporting.Models.Files.Meta;
using FortnitePorting.Exporting.Styles;
using FortnitePorting.Exporting.Types;
using FortnitePorting.Extensions;
using FortnitePorting.Models;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Providers;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using Serilog;
using BaseAssetInfo = FortnitePorting.Models.Assets.Base.BaseAssetInfo;

namespace FortnitePorting.Services;

public class ExportService(
    InfoService info,
    AppService app,
    NavigationService navigation,
    ExportClientService exportClient,
    AssetLoaderService assetLoading,
    SettingsService appSettings) : IService
{
    private ExportSession CreateSession(ExportDataMeta metaData)
        => new(metaData, OpenCustomAssetAvares);

    public async Task ExportTastyRig(ExportDataMeta metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            var serverType = metaData.ExportLocation.ServerType;
            if (serverType is EExportServerType.None)
                return;

            if (!await exportClient.IsRunning(serverType))
            {
                ShowPluginMissing(serverType, metaData.ExportLocation);
                return;
            }

            var session = CreateSession(metaData);
            var exportData = await session.RunAsync([session.CreateTastyExport()]);
            await SendToPluginAsync(serverType, exportData, PluginSettingsFor(metaData.ExportLocation));
        });
    }

    public async Task<bool> Export(Func<ExportSession, IEnumerable<BaseExport>> exportFunction, ExportDataMeta metaData)
    {
        if (metaData.ExportLocation is EExportLocation.CustomFolder && await app.BrowseFolderDialog() is { } path)
        {
            metaData.CustomPath = path;
        }

        var exportedProperly = false;
        await TaskService.RunAsync(async () =>
        {
            var session = CreateSession(metaData);
            var serverType = metaData.ExportLocation.ServerType;

            if (serverType is EExportServerType.None)
            {
                foreach (var export in exportFunction(session))
                {
                    await export.WaitForExports();
                    if (metaData.CancellationToken.IsCancellationRequested) return;
                }
            }
            else
            {
                if (!await exportClient.IsRunning(serverType))
                {
                    ShowPluginMissing(serverType, metaData.ExportLocation);
                    return;
                }

                var exportData = await session.RunAsync(() => exportFunction(session));
                if (metaData.CancellationToken.IsCancellationRequested) return;

                await SendToPluginAsync(serverType, exportData, PluginSettingsFor(metaData.ExportLocation));
            }

            exportedProperly = !metaData.CancellationToken.IsCancellationRequested;
        });

        return exportedProperly;
    }

    public async Task<bool> Export(IEnumerable<BaseAssetInfo> assets, ExportDataMeta metaData)
    {
        return await Export(session => assets.Select(baseAssetInfo =>
        {
            if (baseAssetInfo is AssetInfo assetInfo)
            {
                var asset = assetInfo.Asset;
                var baseStyles = metaData.ExportLocation.IsFolder ? assetInfo.GetAllStyles() : assetInfo.GetSelectedStyles();
                var exportStyles = ConvertStyles(baseStyles);
                var exportType = asset.CreationData.ExportType;

                return CreateExportWithProgress(session, asset.CreationData.DisplayName, asset.CreationData.Object, exportType, exportStyles, metaData);
            }

            if (baseAssetInfo is CustomAssetInfo customAssetInfo)
            {
                var customAsset = customAssetInfo.Asset.Asset;
                return session.CreateCustomMeshExport(customAsset.Name, customAsset.Mesh, customAssetInfo.Asset.CreationData.ExportType);
            }

            return null!;
        }).Where(export => export is not null), metaData);
    }

    public async Task<bool> Export(IEnumerable<ExportFileEntry> assets, ExportDataMeta metaData)
    {
        return await Export(session => assets.Select(entry =>
            CreateExportWithProgress(session, entry.Object.Name, entry.Object, entry.Type, [], metaData, entry.Meta)), metaData);
    }

    public async Task<bool> Export(IEnumerable<UObject> assets, EExportType type, ExportDataMeta metaData)
    {
        return await Export(session => assets.Select(asset =>
            CreateExportWithProgress(session, asset.Name, asset, type, [], metaData)), metaData);
    }

    public async Task<bool> Export(UObject asset, EExportType type, ExportDataMeta metaData)
    {
        return await Export(session =>
        [
            CreateExportWithProgress(session, asset.Outer?.Name.Text.SubstringAfterLast("/") ?? asset.Name, asset, type, [], metaData)
        ], metaData);
    }

    public async Task<bool> Export(UObject asset, ExportDataMeta metaData)
        => await Export(asset, DetermineExportType(asset), metaData);

    public async Task<bool> Export(IEnumerable<UObject> assets, ExportDataMeta metaData)
    {
        var fileEntries = assets.Select(asset => new ExportFileEntry
        {
            Object = asset,
            Type = DetermineExportType(asset)
        });

        return await Export(fileEntries, metaData);
    }

    public EExportType DetermineExportType(UObject asset)
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
            foreach (var loader in assetLoading.Categories.SelectMany(category => category.Loaders))
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

    public string FixPath(string path) => ExportSession.FixPath(path);

    private BaseExportSettings PluginSettingsFor(EExportLocation location)
        => appSettings.ExportSettings.GetSettingsViewModel(location);

    private BaseExport CreateExportWithProgress(
        ExportSession session,
        string displayName,
        UObject asset,
        EExportType exportType,
        ExportStyleBase[] styles,
        ExportDataMeta metaData,
        IExportFileMeta? fileMeta = null)
    {
        var path = asset.GetPathName();
        info.Message(displayName, asset.Name, id: path, autoClose: false);

        ExportProgressUpdate updateDelegate = (name, current, total) =>
        {
            info.UpdateMessage(path, name);
            info.UpdateMessageProgress(path, current, total);
            Log.Information("{DisplayName} - {Current} / {Total}: {Name}", displayName, current, total, name);
        };

        metaData.UpdateProgress += updateDelegate;

        try
        {
            return session.CreateExport(displayName, asset, exportType, styles, fileMeta);
        }
        finally
        {
            info.CloseMessage(id: path);
            metaData.UpdateProgress -= updateDelegate;
        }
    }

    private async Task SendToPluginAsync(EExportServerType serverType, ExportData exportData, BaseExportSettings pluginSettings)
    {
        var wirePayload = new
        {
            MetaData = new
            {
                exportData.MetaData.Version,
                exportData.MetaData.AssetsRoot,
                Settings = pluginSettings
            },
            exportData.Exports
        };

        await exportClient.SendExportAsync(serverType, wirePayload);
    }

    private void ShowPluginMissing(EExportServerType serverType, EExportLocation exportLocation)
    {
        var serverName = serverType.Description;
        info.Message($"{serverName} Server", $"The {serverName} Plugin for Fortnite Porting is not currently installed or running.",
            severity: InfoBarSeverity.Error, closeTime: 3.0f,
            useButton: true, buttonTitle: "Install Plugin", buttonCommand: () =>
            {
                navigation.App.Open<PluginView>();
                navigation.Plugin.Open(exportLocation);
            });
    }

    private static ExportStyleBase[] ConvertStyles(BaseStyleData[] styles)
    {
        return styles.Select<BaseStyleData, ExportStyleBase>(style => style switch
        {
            AssetColorStyleData colorStyle => new ExportColorStyle
            {
                StyleData = colorStyle.StyleData,
                ColorData = colorStyle.ColorData,
                IsParamSet = colorStyle.IsParamSet
            },
            AssetStyleData assetStyle => new ExportStructStyle
            {
                StyleData = assetStyle.StyleData
            },
            ObjectStyleData objStyle => new ExportObjectStyle
            {
                StyleData = objStyle.StyleData,
                AssociatedExportType = objStyle.AssociatedExportType
            },
            _ => throw new NotSupportedException($"Unknown style type: {style.GetType().Name}")
        }).ToArray();
    }
    
    private static Stream OpenCustomAssetAvares(string path)
    {
        var uri = path.StartsWith("avares://", StringComparison.OrdinalIgnoreCase)
            ? path
            : $"avares://FortnitePorting/{path}";
        return AssetLoader.Open(new Uri(uri));
    }
}
