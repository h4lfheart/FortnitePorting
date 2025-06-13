using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Extensions;

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

    public override object ProvideValue(IServiceProvider? serviceProvider)
    {
        if (serviceProvider?.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget service)
        {
            return Margin * Factor * Default;
        }

        return service.TargetProperty switch
        {
            StyledProperty<Thickness> => Margin * Factor * Default,
            StyledProperty<GridLength> => new GridLength(Factor * Default),
            _ => Margin * Factor * Default
        };
    }
    
    public static Thickness Space(double factor)
    {
        return (Thickness) new SpaceExtension(factor).ProvideValue(null);
    }
    
    public static Thickness Space(double horizontal, double vertical)
    {
        return (Thickness) new SpaceExtension(horizontal, vertical).ProvideValue(null);
    }
    
    public static Thickness Space(double left, double top, double right, double bottom)
    {
        return (Thickness) new SpaceExtension(left, top, right, bottom).ProvideValue(null);
    }
}