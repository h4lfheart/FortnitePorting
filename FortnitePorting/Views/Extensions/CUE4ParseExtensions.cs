using System.Linq;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Views.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetOrDefault<T>(this UObject obj, params string[] names)
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

    public static BitmapSource ToBitmapSource(this UTexture2D texture) => texture.Decode()?.ToBitmapSource();

    public static FName? GetValueOrDefault(this FGameplayTagContainer tags, string category, FName def = default)
    {
        return tags.GameplayTags is not { Length: > 0 } ? def : tags.GameplayTags.FirstOrDefault(it => it.Text.StartsWith(category), def);
    }
}