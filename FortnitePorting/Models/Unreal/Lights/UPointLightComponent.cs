using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Models.Unreal.Lights;

public class UPointLightComponent : ULocalLightComponent
{
    [UProperty] public bool bUseInverseSquaredFalloff;
    [UProperty] public float LightFalloffExponent;
    [UProperty] public float SourceRadius;
    [UProperty] public float SoftSourceRadius;
    [UProperty] public float SourceLength;
}