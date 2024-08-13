using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Extensions;

public static class CUE4ParseExtensions
{
    
    public static bool TryLoadEditorData<T>(this UObject asset, out T? editorData) where T : UObject
    {
        var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
        if (CUE4ParseVM.OptionalProvider.TryLoadObjectExports(path, out var exports))
        {
            editorData = exports.FirstOrDefault() as T;
            return editorData is not null;
        }

        editorData = default;
        return false;
    }
}