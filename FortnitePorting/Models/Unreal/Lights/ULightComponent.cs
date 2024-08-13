using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal.Lights;

public class ULightComponent : ULightComponentBase
{
    [UProperty] public float Temperature;
    [UProperty] public float MaxDrawDistance;
    [UProperty] public float MaxDistanceFadeRange;
    [UProperty] public bool bUseTemperature;
    [UProperty] public float SpecularScale;
    [UProperty] public float ShadowResolutionScale;
    [UProperty] public float ShadowBias;
    [UProperty] public float ShadowSlopeBias;
    [UProperty] public float ShadowSharpen;
    [UProperty] public FPackageIndex IESTexture;
}