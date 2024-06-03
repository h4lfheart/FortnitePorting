namespace FortnitePorting.Shared.Models;

public class ExportAttribute(EPrimitiveExportType type) : Attribute
{
    public EPrimitiveExportType ExportType = type;
}

public static class ExportExtensions
{
    public static EPrimitiveExportType GetPrimitiveType(this Enum value)
    {
        var attribute = value
            .GetType()
            .GetField(value.ToString())?
            .GetCustomAttributes(typeof(ExportAttribute), false)
            .SingleOrDefault() as ExportAttribute;
        return attribute.ExportType;
    }
}
