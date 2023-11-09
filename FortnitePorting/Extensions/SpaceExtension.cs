using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Extensions;

public class SpaceExtension : MarkupExtension
{
    private readonly double Factor;
    private const int Default = 8;

    public SpaceExtension()
    {
        Factor = 1;
    }
    
    public SpaceExtension(string expression)
    {
        var parsed = double.TryParse(expression, NumberStyles.Any, new NumberFormatInfo { NumberDecimalSeparator = "." }, out Factor);
        if (!parsed) Factor = 0;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget service)
            throw new InvalidOperationException();

        return service.TargetProperty switch
        {
            StyledProperty<Thickness> =>  new Thickness(Factor * Default),
            StyledProperty<GridLength> => new GridLength(Factor * Default),
            _ => throw new InvalidOperationException()
        };
    }
}