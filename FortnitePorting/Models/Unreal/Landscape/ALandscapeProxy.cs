using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal.Landscape;

public class ALandscapeProxy : AActor
{
    [UProperty] public FPackageIndex[] LandscapeComponents = [];
    [UProperty] public int ComponentSizeQuads;
    [UProperty] public int SubsectionSizeQuads;
    [UProperty] public int NumSubsections;
    [UProperty] public int LandscapeSectionOffset;
    [UProperty] public FPackageIndex? LandscapeMaterial;
    [UProperty] public FPackageIndex? SplineComponent;
    [UProperty] public FGuid LandscapeGuid;
}