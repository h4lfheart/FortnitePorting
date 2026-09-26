using System;
using System.Linq;
using Material.Icons;

namespace FortnitePorting.Models;

public class ExportAttribute(EPrimitiveExportType type) : Attribute
{
    public EPrimitiveExportType ExportType = type;
}

public class NonAssetAttribute : Attribute;
public class CosmeticAssetAttribute : Attribute;
public class DisabledAttribute : Attribute;

public class IconAttribute(MaterialIconKind icon) : Attribute
{
    public MaterialIconKind Icon = icon;
}

public static class ExportExtensions
{
    extension(EExportType value)
    {
        public EPrimitiveExportType PrimitiveType
        {
            get
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
}
