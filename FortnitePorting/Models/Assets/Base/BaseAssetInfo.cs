using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Assets.Base;

public abstract partial class BaseAssetInfo : ObservableObject
{
    [ObservableProperty] private BaseAssetItem _asset;
    [ObservableProperty] private ObservableCollection<AssetStyleInfo> _styleInfos = [];
}