using System;
using System.Linq;

namespace FortnitePorting.Models;

public class ExportAttribute(EPrimitiveExportType type) : Attribute
{
    public EPrimitiveExportType ExportType = type;
}

public static class ExportExtensions
{
    extension(EExportType value)
    {
        public EPrimitiveExportType GetPrimitiveType()
        {
            var attribute = value
                .GetType()
                .GetField(value.ToString())?
                .GetCustomAttributes(typeof(ExportAttribute), false)
                .SingleOrDefault() as ExportAttribute;
            
            return attribute?.ExportType ?? 0;
        }
    }
}
