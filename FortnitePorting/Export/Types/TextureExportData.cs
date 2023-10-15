using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Export.Types;

public class TextureExportData : ExportDataBase
{
    public TextureExportData(string name, UObject asset, EAssetType type, EExportType exportType) : base(name, asset, type, exportType)
    {
    }
}