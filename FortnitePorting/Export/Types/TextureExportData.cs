using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;

namespace FortnitePorting.Export.Types;

public class TextureExportData : ExportDataBase
{
    public TextureExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportType exportType) : base(name, asset, styles, type, exportType)
    {
    }
}