using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.CUE4Parse.Models.Unreal.Lights;

public class ULocalLightComponent : ULightComponent
{
    [UProperty] public float InverseExposureBlend;
    [UProperty] public float AttenuationRadius;
}