using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using DynamicData.Binding;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using Material.Icons;

namespace FortnitePorting.ViewModels;

public partial class AssetsViewModel() : ViewModelBase
{
    [ObservableProperty] private AssetLoaderService _assetLoader;
    [ObservableProperty] private APIService _api;
    [ObservableProperty] private SupabaseService _supabase;

    public AssetsViewModel(AssetLoaderService assetLoader, APIService api, SupabaseService supabase) : this()
    {
        AssetLoader = assetLoader;
        Api = api;
        Supabase = supabase;
        
        AssetLoader.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AssetLoaderService.ActiveLoader))
            {
                OnPropertyChanged(nameof(IsTastyRigApplyVisible));
            }
        };
    }

    public bool IsTastyRigApplyVisible => ExportLocation is EExportLocation.Blender && AssetLoader.ActiveLoader?.Type is EExportType.Outfit;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsTastyRigApplyVisible))]
    private EExportLocation _exportLocation = EExportLocation.Blender;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowNamesIcon))] private bool _showNames = AppSettings.Application.ShowAssetNames;

    public MaterialIconKind ShowNamesIcon => ShowNames ? MaterialIconKind.TextLong : MaterialIconKind.TextShort;
    
    [ObservableProperty] private ObservableCollection<ISidebarItem> _sidebarItems = [];
    
    public override async Task Initialize()
    {
        await TaskService.RunDispatcherAsync(() =>
        {
            foreach (var (index, category) in AssetLoader.Categories.Index())
            {
                var group = new SidebarItemGroup(category.Category.Description.ToUpper())
                {
                    IsExpanded = index == 0
                };

                foreach (var loader in category.Loaders)
                {
                    group.Items.Add(new SidebarItemButton(
                        text: loader.Type.Description,
                        iconBitmap: ImageExtensions.AvaresBitmap($"avares://FortnitePorting/Assets/FN/{loader.Type.ToString()}.png"),
                        tag: loader.Type
                    ));
                }

                SidebarItems.Add(group);

                if (index < AssetLoader.Categories.Count - 1)
                    SidebarItems.Add(new SidebarItemSeparator());
            }
        });

        Navigation.Assets.Open(AppSettings.Application.UseDefaultExportLoadType
            ? AppSettings.Application.DefaultExportLoadType
            : EExportType.Outfit);
    }

    public override async Task OnViewOpened()
    {
        if (AssetLoader.ActiveLoader is null) return;

        Navigation.Assets.Open(AssetLoader.ActiveLoader.Type);
    }

    public override async Task OnViewExited()
    {
        AppSettings.Application.ShowAssetNames = ShowNames;
    }

    [RelayCommand]
    public async Task SetExportLocation(EExportLocation location)
    {
        ExportLocation = location;
    }

    [RelayCommand]
    public async Task Export()
    {
        if (AssetLoader.ActiveLoader is null) return;
        
        AssetLoader.ActiveLoader.Pause();
        
        var exportedProperly = await Exporter.Export(AssetLoader.ActiveLoader.SelectedAssetInfos, AppSettings.ExportSettings.CreateExportMeta(ExportLocation));
        if (exportedProperly && SupaBase.IsLoggedIn)
        {
            await SupaBase.PostExports([
                ..AssetLoader.ActiveLoader.SelectedAssetInfos
                    .OfType<AssetInfo>()
                    .Select(asset => asset.Asset.CreationData.Object.GetPathName()),
                ..AssetLoader.ActiveLoader.SelectedAssetInfos
                    .OfType<CustomAssetInfo>()
                    .Select(asset => $"Custom/{asset.Asset.Asset.Name}"),
            ]);
        }
        
        AssetLoader.ActiveLoader.Unpause();
    }
    
    [RelayCommand]
    public async Task Favorite()
    {
        foreach (var info in AssetLoader.ActiveLoader.SelectedAssetInfos)
        {
            info.Asset.Favorite();
        }
    }
    
    [RelayCommand]
    public async Task ExportTastyRig()
    {
        await Exporter.ExportTastyRig(AppSettings.ExportSettings.CreateExportMeta(ExportLocation));
    }

    private const string ExportIconsMessageId = "ExportAllIcons";

    [RelayCommand]
    public async Task ExportAllIcons()
    {
        if (AssetLoader.ActiveLoader is not { FinishedLoading: true } loader) return;
        if (await App.BrowseFolderDialog() is not { } folderPath) return;

        var items = loader.Source.Items.OfType<AssetItem>().ToArray();
        var total = items.Length;
        var cts = new CancellationTokenSource();

        Info.Message("Exporting Icons", string.Empty, autoClose: false, id: ExportIconsMessageId,
            useButton: true, buttonTitle: "Cancel", buttonCommand: cts.Cancel);

        await TaskService.RunAsync(async () =>
        {
            var sw = Stopwatch.StartNew();
            var saved = 0;

            for (var i = 0; i < items.Length; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                var item = items[i];
                var iconPath = item.CreationData.HighResIconPath ?? item.CreationData.LowResIconPath;
                if (iconPath is null) continue;

                var iconName = Path.GetFileNameWithoutExtension(iconPath);
                Info.UpdateMessage(ExportIconsMessageId, $"{iconName}\n{i + 1} / {total}");

                try
                {
                    var texture = await UEParse.Provider.SafeLoadPackageObjectAsync<UTexture2D>(iconPath);
                    using var bitmap = texture?.Decode()?.ToSkBitmap()?.ToWriteableBitmap();
                    if (bitmap is null) continue;

                    bitmap.Save(Path.Combine(folderPath, $"{iconName}.png"));
                    saved++;
                }
                catch
                {
                    // skip items that fail to load/decode
                }
            }

            sw.Stop();
            Info.CloseMessage(ExportIconsMessageId);
            Info.Message("Icons Dumped", $"Exported {saved} assets in {sw.Elapsed.TotalSeconds:F3}s", closeTime: 6);
        });
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(ExportLocation);
    }
}