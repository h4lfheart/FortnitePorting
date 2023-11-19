using FortnitePorting.Export.Types;

namespace FortnitePorting.Export;

public class ExportResponse
{
    public string AssetsFolder;
    public ExportOptionsBase Options;
    public ExportDataBase[] Data;
}