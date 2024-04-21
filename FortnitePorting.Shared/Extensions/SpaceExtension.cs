using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Shared.Extensions;

public class SpaceExtension : MarkupExtension
{
    private readonly double Factor = 1;
    private readonly Thickness Margin = new(1);
    private const int Default = 8;

    public SpaceExtension(double factor)
    {
        Factor = factor;
    }
    
    public SpaceExtension(double horizontal, double vertical)
    {
        Margin = new Thickness(horizontal, vertical);
    }
    
    public SpaceExtension(double left, double top, double right, double bottom)
    {
        Margin = new Thickness(left, top, right, bottom);
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget service)
            throw new InvalidOperationException();

        return service.TargetProperty switch
        {
            StyledProperty<Thickness> => Margin * Factor * Default,
            StyledProperty<GridLength> => new GridLength(Factor * Default),
            _ => throw new InvalidOperationException()
        };
    }
}