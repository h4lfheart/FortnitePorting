using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.Utils;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Exporting;
using FortnitePorting.Exporting.Context;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files;
using FortnitePorting.Exporting.Models.Files.Meta;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel(FilesService filesService) : ViewModelBase
{
    [ObservableProperty] private FilesService _files = filesService;

    [ObservableProperty] private FileBrowserContext _context = new()
    {
        IsDragDropEnabled = true
    };

    [ObservableProperty] private EExportLocation _assetExportLocation = EExportLocation.Blender;
    [ObservableProperty] private EExportLocation _dataExportLocation = EExportLocation.AssetsFolder;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsAssetExportTarget))]
    private EExportTarget _exportTarget = EExportTarget.Asset;

    public bool IsAssetExportTarget => ExportTarget == EExportTarget.Asset;

    public EnumRecord[] FolderExportLocations =>
        Enum.GetValues<EExportLocation>()
            .Where(val => val.IsFolder)
            .Select(val => val.ToEnumRecord())
            .ToArray();

    public override async Task Initialize()
    {
        if (UEParse.Provider is null) return;
        Context.Initialize();
    }

    public override async Task OnViewOpened()
    {
        Discord.Update($"Browsing {UEParse.Provider.Files.Count:N0} Files");
    }

    public void JumpTo(string path)
    {
        Context.JumpTo(path);
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        Navigation.App.Open<ExportSettingsView>();
        Navigation.ExportSettings.Open(AssetExportLocation);
    }

    [RelayCommand]
    public async Task SetAssetExportLocation(EExportLocation location) => AssetExportLocation = location;

    [RelayCommand]
    public async Task SetDataExportLocation(EExportLocation location) => DataExportLocation = location;

    [RelayCommand]
    public async Task Properties()
    {
        var selectedItemPath = Context.UseFlatView
            ? Context.SelectedFlatViewItems.FirstOrDefault()?.Path
            : Context.SelectedFileViewItems.FirstOrDefault(f => f.Type == ENodeType.File)?.FilePath;
        if (selectedItemPath is null) return;

        try
        {
            if (UEParse.Provider.TryLoadObjectExports(selectedItemPath, out var exports))
            {
                var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
                await TaskService.RunDispatcherAsync(() =>
                    PropertiesPreviewWindow.Preview(
                        selectedItemPath.SubstringAfterLast("/").SubstringBefore("."), json));
            }
        }
        catch (Exception)
        {
            Info.Message("Properties", $"Failed to preview {selectedItemPath}");
        }
    }

    [RelayCommand]
    public async Task Preview()
    {
        var selectedPaths = (Context.UseFlatView
            ? Context.SelectedFlatViewItems.Select(f => f.Path)
            : Context.SelectedFileViewItems.Where(f => f.Type == ENodeType.File).Select(f => f.FilePath)).ToList();

        var loadedAssets = new List<UObject>();
        foreach (var path in selectedPaths)
        {
            var basePath = Exporter.FixPath(path);

            UObject? asset;
            if (path.EndsWith(".umap"))
            {
                var package = await UEParse.Provider.LoadPackageAsync(basePath);
                asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
            }
            else
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await UEParse.Provider.SafeLoadPackageObjectAsync(
                    $"{basePath}.{basePath.SubstringAfterLast("/")}_C");
            }

            asset = TransformAssetForPreview(asset);
            if (asset is null) continue;
            loadedAssets.Add(asset);
        }

        if (loadedAssets.Count == 0)
        {
            await Properties();
            return;
        }

        var meshAssets = loadedAssets.Where(x => x is ULevel or UStaticMesh or USkeletalMesh).ToArray();
        if (meshAssets.Length > 0)
        {
            loadedAssets.RemoveMany(meshAssets);
            ModelPreviewWindow.Preview(meshAssets);
        }

        foreach (var asset in loadedAssets)
            await PreviewAsset(asset);
    }

    private UObject? TransformAssetForPreview(UObject? asset) => asset switch
    {
        UVirtualTextureBuilder vtb => vtb.Texture.Load<UVirtualTexture2D>(),
        UPaperSprite sprite => sprite.BakedSourceTexture?.Load<UTexture2D>(),
        UWorld world => world.PersistentLevel.Load<ULevel>(),
        _ => asset
    };

    public async Task PreviewAsset(UObject? asset)
    {
        var name = asset?.Name!;
        asset = TransformAssetForPreview(asset);
        if (asset is null) return;

        switch (asset)
        {
            case UTexture texture:
                TexturePreviewWindow.Preview(name, texture);
                break;
            case UMaterial:
            case UMaterialFunction:
                if (!UEParse.Provider.MountedVfs.Any(vfs => vfs.Name.Contains(".o.")))
                {
                    Info.Message("Material Preview",
                        "Material node-tree data cannot be loaded because UEFN is not installed.",
                        closeTime: 5, severity: InfoBarSeverity.Error);
                    break;
                }
                MaterialPreviewWindow.Preview(asset);
                break;
            case UMaterialInstanceConstant instance:
                Info.Dialog($"Preview {instance.Name}", "What asset type would you like to preview?", buttons:
                [
                    new DialogButton
                    {
                        Text = "Material Properties",
                        Action = () => TaskService.Run(Properties)
                    },
                    new DialogButton
                    {
                        Text = "Material Node Tree",
                        Action = () =>
                        {
                            UUnrealMaterial? parentMaterial = instance;
                            while (parentMaterial is UMaterialInstanceConstant mic)
                                parentMaterial = mic.Parent;
                            if (parentMaterial is not null)
                                MaterialPreviewWindow.Preview(parentMaterial);
                        }
                    }
                ]);
                break;
            case UStaticMesh:
            case USkeletalMesh:
            case ULevel:
                ModelPreviewWindow.Preview([asset]);
                break;
            case USoundWave soundWave:
                SoundPreviewWindow.Preview(soundWave);
                break;
            case USoundCue soundCue:
                SoundCuePreviewWindow.Preview(soundCue);
                break;
            default:
                await Properties();
                break;
        }
    }

    [RelayCommand]
    public async Task Export()
    {
        switch (ExportTarget)
        {
            case EExportTarget.Asset: await ExportAssets(); break;
            case EExportTarget.Properties: await ExportProperties(); break;
            case EExportTarget.RawData: await ExportRawData(); break;
        }
    }

    private async Task ExportAssets()
    {
        var unsupportedExportTypes = new HashSet<string>();

        var paths = Context.UseFlatView
            ? Context.SelectedFlatViewItems.Select(x => x.Path).ToList()
            : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();

        var folders = Context.UseFlatView ? [] : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            if (folder.SourceNode is null) continue;
            paths.AddRange(Context.GetAllFileDescendants(folder.SourceNode, folder)
                .Where(x => x.Type == ENodeType.File)
                .Select(x => x.FilePath));
        }

        var exports = new List<ExportFileEntry>();
        foreach (var path in paths)
        {
            var basePath = Exporter.FixPath(path);
            UObject? asset = null;
            if (path.EndsWith(".umap"))
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                if (asset is not UWorld)
                {
                    var package = await UEParse.Provider.LoadPackageAsync(basePath);
                    asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
                }
            }
            else
            {
                asset = await UEParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await UEParse.Provider.SafeLoadPackageObjectAsync(
                    $"{basePath}.{basePath.SubstringAfterLast("/")}_C");
            }

            if (asset is null) continue;

            var fileEntry = new ExportFileEntry
            {
                Object = asset
            };

            switch (asset)
            {
                case UVirtualTextureBuilder vtb:
                    asset = vtb.Texture.Load<UVirtualTexture2D>();
                    break;
                case UPaperSprite sprite:
                    asset = sprite.BakedSourceTexture.Load<UTexture2D>();
                    break;
                case UAnimSequence sequence:
                    if (sequence.AdditiveAnimType is not EAdditiveAnimationType.AAT_None && sequence.RefPoseSeq is null)
                    {
                        if (await FilePickerWindow.OpenBrowserAsync(windowName: "Select Additive Base Sequence", startPath: UEParse.Provider.FixPath(sequence.GetPathName())) is { Length: > 0 } selectedPaths
                            && selectedPaths.FirstOrDefault() is { } selectedPath
                            && UEParse.Provider.TryLoadPackageObject<UAnimSequence>(Exporter.FixPath(selectedPath), out var baseSequence))
                        {
                            fileEntry.Meta = new ExportAdditiveAnimFileMeta
                            {
                                BaseSequence = baseSequence
                            };
                        }
                        else
                        {
                            Info.Message("Additive Animation", "A valid base pose was not selected, animation export result may be inaccurate.");
                        }
                    }
                    break;
            }

            var exportType = Exporter.DetermineExportType(asset);
            if (exportType is EExportType.None)
            {
                unsupportedExportTypes.Add(asset.ExportType);
                continue;
            }
            
            fileEntry.Type = exportType;

            exports.Add(fileEntry);
        }

        if (exports.Count == 0)
        {
            Info.Message("Exporter",
                unsupportedExportTypes.Count == 0
                    ? "Failed to load any assets for export."
                    : $"Assets with these types do not have exporters: {unsupportedExportTypes.CommaJoin()}.",
                InfoBarSeverity.Warning);
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(AssetExportLocation);
        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids | EWorldFlags.HLODs;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;

        var exportedProperly = await Exporter.Export(exports, meta);
        if (exportedProperly && SupaBase.IsLoggedIn)
            await SupaBase.PostExports(exports.Select(e => e.Object.GetPathName()));
    }

    private async Task ExportProperties()
    {
        var paths = Context.UseFlatView
            ? Context.SelectedFlatViewItems.Select(x => x.Path).ToList()
            : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();

        var folders = Context.UseFlatView ? [] : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            if (folder.SourceNode is null) continue;
            paths.AddRange(Context.GetAllFileDescendants(folder.SourceNode, folder)
                .Where(x => x.Type == ENodeType.File)
                .Select(x => x.FilePath));
        }

        if (paths.Count == 0)
        {
            Info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(DataExportLocation);
        if (meta.ExportLocation is EExportLocation.CustomFolder &&
            await App.BrowseFolderDialog() is { } customExportPath)
            meta.CustomPath = customExportPath;

        var context = new ExportContext(meta);
        foreach (var path in paths)
        {
            if (!UEParse.Provider.TryLoadObjectExports(path, out var exports)) continue;

            var exportPath = context.BuildExportPath(
                meta.CustomPath is not null
                    ? path.SubstringAfterLast("/").SubstringBeforeLast(".")
                    : path, "json");

            Log.Information("Exporting Properties: {ExportPath}", exportPath);
            var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
            await File.WriteAllTextAsync(exportPath, json);
        }
    }

    private async Task ExportRawData()
    {
        var paths = Context.UseFlatView
            ? Context.SelectedFlatViewItems.Select(x => x.Path).ToList()
            : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.File).Select(x => x.FilePath).ToList();

        var folders = Context.UseFlatView ? [] : Context.SelectedFileViewItems.Where(x => x.Type == ENodeType.Folder);
        foreach (var folder in folders)
        {
            if (folder.SourceNode is null) continue;
            paths.AddRange(Context.GetAllFileDescendants(folder.SourceNode, folder)
                .Where(x => x.Type == ENodeType.File)
                .Select(x => x.FilePath));
        }

        if (paths.Count == 0)
        {
            Info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        var meta = AppSettings.ExportSettings.CreateExportMeta(DataExportLocation);
        if (meta.ExportLocation is EExportLocation.CustomFolder &&
            await App.BrowseFolderDialog() is { } customExportPath)
            meta.CustomPath = customExportPath;

        var exportContext = new ExportContext(meta);
        foreach (var path in paths)
        {
            if (!UEParse.Provider.TrySavePackage(path, out var assets)) continue;

            foreach (var (assetPath, assetData) in assets)
            {
                var exportPath = exportContext.BuildExportPath(
                    meta.CustomPath is not null
                        ? assetPath.SubstringAfterLast("/").SubstringBeforeLast(".")
                        : assetPath, assetPath.SubstringAfterLast("."));

                Log.Information("Exporting Raw Data: {ExportPath}", exportPath);
                await File.WriteAllBytesAsync(exportPath, assetData);
            }
        }
    }
}