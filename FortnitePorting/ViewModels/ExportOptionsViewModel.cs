using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using Material.Icons;
using ReactiveUI;
using Serilog;
using AssetItem = FortnitePorting.Controls.Assets.AssetItem;

namespace FortnitePorting.ViewModels;

public partial class ExportOptionsViewModel : ViewModelBase
{
    [ObservableProperty] private BlenderExportOptions blender = new();
    [ObservableProperty] private UnrealExportOptions unreal = new();
    [ObservableProperty] private FolderExportOptions folder = new();

    public ExportOptionsBase Get(EExportType type)
    {
        return type switch
        {
            EExportType.Blender => Blender,
            EExportType.Unreal => Unreal,
            EExportType.Folder => Folder
        };
    }

    [RelayCommand]
    public async Task BrowseExportPath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            Folder.ExportFolder = path;
        }
    }
}

public partial class ExportOptionsBase : ObservableObject
{
    [ObservableProperty] private EMeshExportTypes meshExportType = EMeshExportTypes.UEFormat;
    
    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;
    

    public virtual ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions();
    }
}

public partial class BlenderExportOptions : ExportOptionsBase
{
    [ObservableProperty] private bool scaleDown = true;
    [ObservableProperty] private bool importCollection = true;
    
    [ObservableProperty] private ERigType rigType = ERigType.Default;
    [ObservableProperty] private bool mergeSkeletons = true;
    [ObservableProperty] private bool reorientBones = false;
    [ObservableProperty] private bool hideFaceBones = false;
    [ObservableProperty] private float boneLength = 0.4f;
    
    [ObservableProperty] private ESupportedLODs levelOfDetail = ESupportedLODs.LOD0;
    [ObservableProperty] private bool useQuads = false;
    [ObservableProperty] private bool preserveVolume = false;
    
    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;
    
    [ObservableProperty] private bool importSounds = true;
    [ObservableProperty] private bool loopAnimation = true;
    [ObservableProperty] private bool updateTimelineLength = true;
    
    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LevelOfDetail is ESupportedLODs.LOD0 ? ELodFormat.FirstLod : ELodFormat.AllLods,
            MeshFormat = MeshExportType switch
            {
                EMeshExportTypes.UEFormat => EMeshFormat.UEFormat,
                EMeshExportTypes.ActorX => EMeshFormat.ActorX
            },
            CompressionFormat = EFileCompressionFormat.ZSTD,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}

public partial class UnrealExportOptions : ExportOptionsBase
{
    [ObservableProperty] private ESupportedLODs levelOfDetail = ESupportedLODs.LOD0;
    
    [ObservableProperty] private bool forUEFN = true;
    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;
    
    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LevelOfDetail is ESupportedLODs.LOD0 ? ELodFormat.FirstLod : ELodFormat.AllLods,
            MeshFormat = MeshExportType switch
            {
                EMeshExportTypes.UEFormat => EMeshFormat.UEFormat,
                EMeshExportTypes.ActorX => EMeshFormat.ActorX
            },
            CompressionFormat = EFileCompressionFormat.ZSTD,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}

public partial class FolderExportOptions : ExportOptionsBase
{
    [ObservableProperty] private string exportFolder = "???";
    
    [ObservableProperty] private ELodFormat lodFormat = ELodFormat.FirstLod;

    public override ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions
        {
            LodFormat = LodFormat,
            MeshFormat = MeshExportType switch
            {
                EMeshExportTypes.UEFormat => EMeshFormat.UEFormat,
                EMeshExportTypes.ActorX => EMeshFormat.ActorX
            },
            CompressionFormat = EFileCompressionFormat.ZSTD,
            ExportMorphTargets = true,
            ExportMaterials = false
        };
    }
}