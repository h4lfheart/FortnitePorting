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
using FortnitePorting.CUE4Parse.Models.Unreal;
using FortnitePorting.CUE4Parse.Models.Unreal.VirtualTexture;
using FortnitePorting.Exporting;
using FortnitePorting.Exporting.Context;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files;
using FortnitePorting.Exporting.Models.Files.Meta;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Models;
using FortnitePorting.Models.Files;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class FilesViewModel(
    FilesService files,
    ExportService exporter,
    CUE4ParseService ueParse,
    InfoService info,
    NavigationService navigation,
    SettingsService settings,
    DiscordService discord,
    SupabaseService supabase,
    AppService app) : ViewModelBase, IResettable
{
    [ObservableProperty] private FilesService _files = files;

    private readonly ExportService _exporter = exporter;
    private readonly CUE4ParseService _ueParse = ueParse;
    private readonly InfoService _info = info;
    private readonly NavigationService _navigation = navigation;
    private readonly SettingsService _settings = settings;
    private readonly DiscordService _discord = discord;
    private readonly SupabaseService _supaBase = supabase;
    private readonly AppService _app = app;

    [ObservableProperty] private FileBrowserContext _context = new()
    {
        IsDragDropEnabled = true
    };

    [ObservableProperty] private EExportLocation _assetExportLocation = EExportLocation.Blender;
    [ObservableProperty] private EExportLocation _dataExportLocation = EExportLocation.AssetsFolder;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsAssetExportTarget)), NotifyPropertyChangedFor(nameof(ShowAssetExportButton)), NotifyPropertyChangedFor(nameof(ShowDataExportButton))]
    private EExportTarget _exportTarget = EExportTarget.Asset;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowAssetExportButton)), NotifyPropertyChangedFor(nameof(ShowDataExportButton))]
    private bool _isExporting;

    public bool IsAssetExportTarget => ExportTarget == EExportTarget.Asset;
    public bool ShowAssetExportButton => IsAssetExportTarget && !IsExporting;
    public bool ShowDataExportButton => !IsAssetExportTarget && !IsExporting;

    private ExportDataMeta? _exportMeta;

    public EnumRecord[] FolderExportLocations =>
        Enum.GetValues<EExportLocation>()
            .Where(val => val.IsFolder)
            .Select(val => val.ToEnumRecord())
            .ToArray();

    public void Reset()
    {
        Context.Reset();
        Context = new FileBrowserContext { IsDragDropEnabled = true };
        InvalidateInitialization();
    }

    public override async Task Initialize()
    {
        var defaultLocation = _settings.Application.DefaultExportLocation;
        AssetExportLocation = defaultLocation;
        if (defaultLocation.IsFolder)
            DataExportLocation = defaultLocation;

        if (_ueParse.Provider is null) return;
        Context.Initialize();
    }

    public override async Task OnViewOpened()
    {
        _discord.Update($"Browsing {_ueParse.Provider.Files.Count:N0} Files");
    }

    public void JumpTo(string path)
    {
        Context.JumpTo(path);
    }

    [RelayCommand]
    public async Task OpenSettings()
    {
        _navigation.App.Open<ExportSettingsView>();
        _navigation.ExportSettings.Open(AssetExportLocation);
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
            if (_ueParse.Provider.TryLoadObjectExports(selectedItemPath, out var exports))
            {
                var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
                await TaskService.RunDispatcherAsync(() =>
                    PropertiesPreviewWindow.Preview(
                        selectedItemPath.SubstringAfterLast("/").SubstringBefore("."), json));
            }
        }
        catch (Exception)
        {
            _info.Message("Properties", $"Failed to preview {selectedItemPath}");
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
            var basePath = _exporter.FixPath(path);

            UObject? asset;
            if (path.EndsWith(".umap"))
            {
                var package = await _ueParse.Provider.LoadPackageAsync(basePath);
                asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
            }
            else
            {
                asset = await _ueParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await _ueParse.Provider.SafeLoadPackageObjectAsync(
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
                if (!_ueParse.Provider.MountedVfs.Any(vfs => vfs.Name.Contains(".o.")))
                {
                    _info.Message("Material Preview",
                        "Material node-tree data cannot be loaded because UEFN is not installed.",
                        closeTime: 5, severity: InfoBarSeverity.Error);
                    break;
                }
                MaterialPreviewWindow.Preview(asset);
                break;
            case UMaterialInstanceConstant instance:
                _info.Dialog($"Preview {instance.Name}", "What asset type would you like to preview?", buttons:
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
            case UAnimationAsset animation:
                PreviewAnimation(animation);
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

    private void PreviewAnimation(UAnimationAsset animation)
    {
        if (ModelPreviewWindow.TryApplyAnimation(animation))
            return;

        _info.Dialog($"Preview {animation.Name}", "Choose a skeletal mesh to preview this animation.", buttons:
        [
            new DialogButton
            {
                Text = "Use Default Mannequin",
                Action = () => TaskService.Run(async () =>
                    await PreviewAnimationWithMeshAsync(animation, DefaultMannequinMeshPath))
            },
            new DialogButton
            {
                Text = "Select Mesh",
                Action = () => TaskService.RunDispatcher(async () =>
                {
                    if (await FilePickerWindow.OpenBrowserAsync("Select Skeletal Mesh") is not { Length: > 0 } paths)
                        return;

                    await PreviewAnimationWithMeshAsync(animation, _exporter.FixPath(paths[0]));
                })
            }
        ]);
    }

    private async Task PreviewAnimationWithMeshAsync(UAnimationAsset animation, string meshPath)
    {
        var mesh = await _ueParse.Provider.SafeLoadPackageObjectAsync<USkeletalMesh>(meshPath);
        mesh ??= await _ueParse.Provider.SafeLoadPackageObjectAsync(meshPath) as USkeletalMesh;
        if (mesh is null)
        {
            await TaskService.RunDispatcherAsync(() =>
                _info.Message("Model Viewer", "Could not load the selected skeletal mesh.",
                    severity: InfoBarSeverity.Warning));
            return;
        }

        await TaskService.RunDispatcherAsync(() => ModelPreviewWindow.Preview([mesh], animation));
    }

    private const string DefaultMannequinMeshPath =
        "FortniteGame/Content/Creative/Devices/Mannequin/Meshes/CP_Device_Mannequin";

    [RelayCommand]
    public async Task Export()
    {
        var location = ExportTarget is EExportTarget.Asset ? AssetExportLocation : DataExportLocation;
        _exportMeta = _settings.ExportSettings.CreateExportMeta(location);
        IsExporting = true;

        try
        {
            switch (ExportTarget)
            {
                case EExportTarget.Asset: await ExportAssets(_exportMeta); break;
                case EExportTarget.Properties: await ExportProperties(_exportMeta); break;
                case EExportTarget.RawData: await ExportRawData(_exportMeta); break;
            }
        }
        finally
        {
            _exportMeta?.Dispose();
            _exportMeta = null;
            IsExporting = false;
        }
    }

    [RelayCommand]
    public void CancelExport()
    {
        _exportMeta?.Cancel();
    }

    private async Task ExportAssets(ExportDataMeta meta)
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
            var basePath = _exporter.FixPath(path);
            UObject? asset = null;
            if (path.EndsWith(".umap"))
            {
                asset = await _ueParse.Provider.SafeLoadPackageObjectAsync(basePath);
                if (asset is not UWorld)
                {
                    var package = await _ueParse.Provider.LoadPackageAsync(basePath);
                    asset = package.GetExports().OfType<UWorld>().FirstOrDefault();
                }
            }
            else
            {
                asset = await _ueParse.Provider.SafeLoadPackageObjectAsync(basePath);
                asset ??= await _ueParse.Provider.SafeLoadPackageObjectAsync(
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
                        if (await FilePickerWindow.OpenBrowserAsync(windowName: "Select Additive Base Sequence", startPath: _ueParse.Provider.FixPath(sequence.GetPathName())) is { Length: > 0 } selectedPaths
                            && selectedPaths.FirstOrDefault() is { } selectedPath
                            && _ueParse.Provider.TryLoadPackageObject<UAnimSequence>(_exporter.FixPath(selectedPath), out var baseSequence))
                        {
                            fileEntry.Meta = new ExportAdditiveAnimFileMeta
                            {
                                BaseSequence = baseSequence
                            };
                        }
                        else
                        {
                            _info.Message("Additive Animation", "A valid base pose was not selected, animation export result may be inaccurate.");
                        }
                    }
                    break;
            }

            var exportType = _exporter.DetermineExportType(asset);
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
            _info.Message("Exporter",
                unsupportedExportTypes.Count == 0
                    ? "Failed to load any assets for export."
                    : $"Assets with these types do not have exporters: {unsupportedExportTypes.CommaJoin()}.",
                InfoBarSeverity.Warning);
            return;
        }

        meta.WorldFlags = EWorldFlags.Actors | EWorldFlags.Landscape | EWorldFlags.WorldPartitionGrids | EWorldFlags.HLODs;
        if (meta.Settings.ImportInstancedFoliage)
            meta.WorldFlags |= EWorldFlags.InstancedFoliage;

        var exportedProperly = await _exporter.Export(exports, meta);
        if (exportedProperly && _supaBase.IsLoggedIn)
            await _supaBase.PostExports(exports.Select(e => e.Object.GetPathName()));
    }

    private async Task ExportProperties(ExportDataMeta meta)
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
            _info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        if (meta.ExportLocation is EExportLocation.CustomFolder &&
            await _app.BrowseFolderDialog() is { } customExportPath)
            meta.CustomPath = customExportPath;

        var context = new ExportContext(meta);
        foreach (var path in paths)
        {
            if (!_ueParse.Provider.TryLoadObjectExports(path, out var exports)) continue;

            var exportPath = context.BuildExportPath(
                meta.CustomPath is not null
                    ? path.SubstringAfterLast("/").SubstringBeforeLast(".")
                    : path, "json");

            Log.Information("Exporting Properties: {ExportPath}", exportPath);
            var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
            await File.WriteAllTextAsync(exportPath, json);
        }
    }

    private async Task ExportRawData(ExportDataMeta meta)
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
            _info.Message("Exporter", "Failed to load any assets for export.", InfoBarSeverity.Warning);
            return;
        }

        if (meta.ExportLocation is EExportLocation.CustomFolder &&
            await _app.BrowseFolderDialog() is { } customExportPath)
            meta.CustomPath = customExportPath;

        var exportContext = new ExportContext(meta);
        foreach (var path in paths)
        {
            if (!_ueParse.Provider.TrySavePackage(path, out var assets)) continue;

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