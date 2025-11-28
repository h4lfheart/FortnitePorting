using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Objects.Core.Math;
using EpicManifestParser.UE;

namespace FortnitePorting.Models.Unreal.Lights;

public class ULightComponentBase : USceneComponent
{
    [UProperty] public FGuid LightGuid;
    [UProperty] public float Intensity;
    [UProperty] public FColor LightColor;
    [UProperty] public bool CastShadows;
    [UProperty] public bool CastStaticShadows;
    [UProperty] public bool CastDynamicShadows;
    [UProperty] public bool IndirectLightingIntensity;
    [UProperty] public bool VolumetricScatteringIntensity;
}