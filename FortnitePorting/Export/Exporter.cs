using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.Assets;
using FortnitePorting.Export.Models;
using FortnitePorting.Export.Types;
using FortnitePorting.Models.API;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;

namespace FortnitePorting.Export;

public static class Exporter
{
    public static async Task Export(IEnumerable<AssetInfo> assets, ExportDataMeta metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            if (await ApiVM.FortnitePortingServer.PingAsync(EExportServerType.Blender) is false)
            {
                AppVM.Message("Blender Server", "The blender server for Fortnite Porting is not currently running.", InfoBarSeverity.Error, false);
                return;
            }
            
            
            var exports = assets.Select(asset => CreateExport(asset, metaData)).ToArray();
            foreach (var export in exports) export.WaitForExports();
            
            var exportData = new ExportData
            {
                MetaData = metaData,
                Exports = exports
            };
            
            var data = JsonConvert.SerializeObject(exportData);
            await ApiVM.FortnitePortingServer.SendAsync(data, EExportServerType.Blender);
        });
    }
    
    public static async Task Export(List<KeyValuePair<UObject, EExportType>> assets, ExportDataMeta metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            if (await ApiVM.FortnitePortingServer.PingAsync(EExportServerType.Blender) is false)
            {
                AppVM.Message("Blender Server", "The blender server for Fortnite Porting is not currently running.", InfoBarSeverity.Error, false);
                return;
            }
            
            
            var exports = assets.Select(kvp => CreateExport(kvp.Key, kvp.Value, metaData)).ToArray();
            foreach (var export in exports) export.WaitForExports();
            
            var exportData = new ExportData
            {
                MetaData = metaData,
                Exports = exports
            };
            
            var data = JsonConvert.SerializeObject(exportData);
            await ApiVM.FortnitePortingServer.SendAsync(data, EExportServerType.Blender);
        });
    }
    
    public static async Task Export(UObject asset, EExportType type, ExportDataMeta metaData)
    {
        await TaskService.RunAsync(async () =>
        {
            if (await ApiVM.FortnitePortingServer.PingAsync(EExportServerType.Blender) is false)
            {
                AppVM.Message("Blender Server", "The blender server for Fortnite Porting is not currently running.", InfoBarSeverity.Error, false);
                return;
            }
            
            
            var export = CreateExport(asset, type, metaData);
            export.WaitForExports();
            
            var exportData = new ExportData
            {
                MetaData = metaData,
                Exports = [export]
            };
            
            var data = JsonConvert.SerializeObject(exportData);
            await ApiVM.FortnitePortingServer.SendAsync(data, EExportServerType.Blender);
        });
    }
    
    private static BaseExport CreateExport(UObject asset, EExportType exportType, ExportDataMeta metaData)
    {
        return CreateExport(asset.Name, asset, [], exportType, metaData);
    }

    private static BaseExport CreateExport(AssetInfo assetInfo, ExportDataMeta metaData)
    {
        var asset = assetInfo.Data.Asset;
        var styles = assetInfo.Data.GetSelectedStyles();
        var exportType = asset.CreationData.ExportType;
        return CreateExport(asset.CreationData.DisplayName, asset.CreationData.Object, styles, exportType, metaData);
    }
    
    private static BaseExport CreateExport(string name, UObject asset, FStructFallback[] styles, EExportType exportType, ExportDataMeta metaData)
    {
        var primitiveType = exportType.GetPrimitiveType();
        return primitiveType switch
        {
            EPrimitiveExportType.Mesh => new MeshExport(name, asset, styles, exportType, metaData),
            _ => throw new NotImplementedException($"Exporting {primitiveType} assets is not supported yet.")
        };
    }
}