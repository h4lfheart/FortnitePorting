using System;
using System.Globalization;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;

namespace FortnitePorting.Extensions;

public class MaterialKindToGeometry : MarkupExtension
{
    private static readonly MaterialIconKindToGeometryConverter Converter = new();
    private readonly MaterialIconKind EnumValue;

    public MaterialKindToGeometry(MaterialIconKind enumValue)
    {
        EnumValue = enumValue;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var geometry = (Geometry) Converter.Convert(EnumValue, typeof(Geometry), null, CultureInfo.CurrentCulture);
        geometry.Transform = new ScaleTransform(0.5, 0.5);
        return geometry;
    }
}