using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using Material.Icons;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortIcon))]
    private bool _descendingSort = false;

    public MaterialIconKind SortIcon => DescendingSort ? MaterialIconKind.SortDescending : MaterialIconKind.SortAscending;
    
    [ObservableProperty] private EAssetSortType _sortType = EAssetSortType.None;
    [ObservableProperty] private EExportLocation _exportLocation = EExportLocation.Blender;
}