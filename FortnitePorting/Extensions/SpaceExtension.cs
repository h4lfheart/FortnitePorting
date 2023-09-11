using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Extensions;

public class SpaceExtension : MarkupExtension
{
    private readonly double Factor;
    private const int Default = 8;
    
    public SpaceExtension(string expression)
    {
        Factor = double.Parse(expression);
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