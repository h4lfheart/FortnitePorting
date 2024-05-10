using System.Globalization;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using Path = Avalonia.Controls.Shapes.Path;

namespace FortnitePorting.Shared.Extensions;

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