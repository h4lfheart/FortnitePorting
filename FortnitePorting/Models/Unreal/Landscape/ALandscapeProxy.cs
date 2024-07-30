using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal.Landscape;

public class ALandscapeProxy : AActor
{
    public FPackageIndex[] LandscapeComponents;
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public int LandscapeSectionOffset;
    public FPackageIndex? LandscapeMaterial;
    public FPackageIndex? SplineComponent;
    public FGuid LandscapeGuid;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        ComponentSizeQuads = GetOrDefault<int>("ComponentSizeQuads");
        SubsectionSizeQuads = GetOrDefault<int>("SubsectionSizeQuads");
        NumSubsections = GetOrDefault<int>("NumSubsections");
        LandscapeComponents = GetOrDefault("LandscapeComponents", Array.Empty<FPackageIndex>());
        LandscapeSectionOffset = GetOrDefault<int>("LandscapeSectionOffset");
        LandscapeMaterial = GetOrDefault<FPackageIndex>("LandscapeMaterial");
        SplineComponent = GetOrDefault<FPackageIndex>("SplineComponent");
        LandscapeGuid = GetOrDefault<FGuid>("LandscapeGuid");
    }
}