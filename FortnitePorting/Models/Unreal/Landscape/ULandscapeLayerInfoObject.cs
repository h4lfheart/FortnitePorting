using System;
using System.Runtime.InteropServices.JavaScript;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal.Landscape;

public class ULandscapeLayerInfoObject : USceneComponent
{
    [UProperty] public string LayerName;
    [UProperty] public FLinearColor LayerUsageDebugColor;
    [UProperty] public FPackageIndex PhysMaterial;

}