using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FRarityCollection
{
    [UProperty] public FLinearColor Color1;
    [UProperty] public FLinearColor Color2;
    [UProperty] public FLinearColor Color3;
    [UProperty] public FLinearColor Color4;
    [UProperty] public FLinearColor Color5;
    [UProperty] public float Radius;
    [UProperty] public float Falloff;
    [UProperty] public float Brightness;
    [UProperty] public float Roughness;
}