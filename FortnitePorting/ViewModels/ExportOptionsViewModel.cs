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
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
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

    [RelayCommand]
    public async Task BrowseExportPath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            Folder.ExportFolder = path;
        }
    }
}

public partial class BlenderExportOptions : ObservableObject
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
    
    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;
    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;
    
    [ObservableProperty] private bool importSounds = true;
    [ObservableProperty] private bool loopAnimation = true;
    [ObservableProperty] private bool updateTimelineLength = true;
}

public partial class UnrealExportOptions : ObservableObject
{
    [ObservableProperty] private ESupportedLODs levelOfDetail = ESupportedLODs.LOD0;
    
    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private bool forUEFN = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;
    [ObservableProperty] private float ambientOcclusion = 0.0f;
    [ObservableProperty] private float cavity = 0.0f;
    [ObservableProperty] private float subsurface = 0.0f;
}

public partial class FolderExportOptions : ObservableObject
{
    [ObservableProperty] private string exportFolder = "???";
    
    [ObservableProperty] private ELodFormat lodFormat = ELodFormat.FirstLod;
    
    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;
}