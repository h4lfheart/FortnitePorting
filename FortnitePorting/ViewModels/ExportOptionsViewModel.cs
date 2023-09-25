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
    [ObservableProperty] private bool exportMaterials = true;
    [ObservableProperty] private EImageType imageType = EImageType.PNG;
    [ObservableProperty] private BlenderExportOptions blender = new();
}

public partial class BlenderExportOptions : ObservableObject
{
    [ObservableProperty] private bool scaleDown = true;
    [ObservableProperty] private bool importCollection = true;
    
    [ObservableProperty] private ERigType rigType = ERigType.Default;
    [ObservableProperty] private bool mergeSkeletons = true;
    [ObservableProperty] private bool reorientBones = false;
    [ObservableProperty] private bool hideFaceBones = false;
    
    [ObservableProperty] private bool useQuads = false;
    [ObservableProperty] private bool preserveVolume = false;
}