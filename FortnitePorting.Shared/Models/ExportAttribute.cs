namespace FortnitePorting.Shared.Models;

public class ExportAttribute(EExportType type) : Attribute
{
    public EExportType ExportType = type;
}

public static class ExportExtensions
{
    public static EExportType GetExportType(this Enum value)
    {
        var attribute = value
            .GetType()
            .GetField(value.ToString())?
            .GetCustomAttributes(typeof(ExportAttribute), false)
            .SingleOrDefault() as ExportAttribute;
        return attribute.ExportType;
    }
}
