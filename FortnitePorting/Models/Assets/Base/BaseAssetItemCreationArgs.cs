namespace FortnitePorting.Models.Assets.Base;

public class BaseAssetItemCreationArgs
{
    public required string DisplayName { get; set; }
    public required string ID { get; set; }
    public required string Description { get; set; }
    public required EExportType ExportType { get; set; }
    public bool IsHidden { get; set; } = false;
}