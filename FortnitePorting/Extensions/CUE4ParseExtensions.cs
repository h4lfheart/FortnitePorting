using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetAnyOrDefault<T>(this UObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.Properties.Any(x => x.Name.Text.Equals(name)))
            {
                return obj.GetOrDefault<T>(name);
            }
        }

        return default;
    }
    
    public static T GetAnyOrDefault<T>(this FStructFallback obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.Properties.Any(x => x.Name.Text.Equals(name)))
            {
                return obj.GetOrDefault<T>(name);
            }
        }

        return default;
    }
    
    public static FName? GetValueOrDefault(this FGameplayTagContainer tags, string category, FName def = default)
    {
        return tags.GameplayTags is not { Length: > 0 } ? def : tags.GetValue(category);
    }
    
    public static bool ContainsAny(this FGameplayTagContainer tags, params string[] check)
    {
        return check.Any(x => tags.ContainsAny(x));
    }

    public static bool ContainsAny(this FGameplayTagContainer tags, string check)
    {
        return tags.GameplayTags.Any(x => x.TagName.Text.Contains(check)); 
    }
}