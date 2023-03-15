using System;
using System.Collections.Generic;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using Newtonsoft.Json;

namespace FortnitePorting.Models;

public class FortniteWrappedData
{
    public Dictionary<string, FortniteWrappedItem> ItemsExported = new(); // path : count
    public Dictionary<string, FortniteWrappedItem> MusicPlayed = new(); // path : count
    public TimeSpan TimeSpentOpen = TimeSpan.Zero;

    [JsonIgnore] public DateTime InstanceStart;

    public void Music(IExportableAsset? asset)
    {
        if (!AppSettings.Current.TrackWrappedData) return;
        if (asset is null) return;
        var path = asset.Asset.GetPathName();
        MusicPlayed.TryAdd(path, new FortniteWrappedItem(asset.Type));
        MusicPlayed[path].Count++;
    }
    
    public void Asset(IExportableAsset? asset)
    {
        if (!AppSettings.Current.TrackWrappedData) return;
        if (asset is null) return;
        if (asset.Type == EAssetType.Mesh) return;
        var path = asset.Asset.GetPathName();
        ItemsExported.TryAdd(path, new FortniteWrappedItem(asset.Type));
        ItemsExported[path].Count++;
    }
}