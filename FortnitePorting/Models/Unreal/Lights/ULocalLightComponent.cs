using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Models.Unreal.Lights;

public class ULocalLightComponent : ULightComponent
{
    [UProperty] public float InverseExposureBlend;
    [UProperty] public float AttenuationRadius;
}