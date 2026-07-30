using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;

namespace FortnitePorting.Exporting.Styles;

public abstract class ExportStyleBase;

public class ExportObjectStyle : ExportStyleBase
{
    public UObject StyleData = null!;
    public EExportType AssociatedExportType = EExportType.None;
}

public class ExportStructStyle : ExportStyleBase
{
    public FStructFallback StyleData = null!;
}

public class ExportColorStyle : ExportStructStyle
{
    public FStructFallback ColorData = null!;
    public bool IsParamSet;
}
