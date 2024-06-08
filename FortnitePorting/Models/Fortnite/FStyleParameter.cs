using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FStyleParameter<T>
{
    public T Value;
    public FName ParamName;
    public string Name => ParamName.Text;

    public FStyleParameter(FStructFallback fallback)
    {
        ParamName = fallback.GetOrDefault<FName>(nameof(ParamName));
        Value = fallback.GetOrDefault<T>(nameof(Value));
    }
} 
