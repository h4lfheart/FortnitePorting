using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    [ObservableProperty] private EAssetSortType _sortType = EAssetSortType.None;
    
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
}