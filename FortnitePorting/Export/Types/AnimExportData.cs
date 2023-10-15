using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Export.Types;

public class AnimExportData : ExportDataBase
{
    public AnimExportData(string name, UObject asset, EAssetType type, EExportType exportType) : base(name, asset, type, exportType)
    {
    }
}
