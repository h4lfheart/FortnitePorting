using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.CUE4Parse.Models.Fortnite.Styles;

[StructFallback]
public class FStyleParameter<T>
{
    [UProperty] public T Value;
    [UProperty] public FName ParamName;
    public string Name => ParamName.Text;

} 
