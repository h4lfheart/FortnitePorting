using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        return value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false).SingleOrDefault() is not DescriptionAttribute attribute ? value.ToString() : attribute.Description;
    }
}

public class EnumToItemsSource : MarkupExtension
{
    private readonly Type _type;

    public EnumToItemsSource(Type type)
    {
        _type = type;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var values = Enum.GetValues(_type).Cast<Enum>();
        return values.Select(x => x.GetDescription()).ToList();
    }
}

public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        return enumValue?.GetDescription();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var values = Enum.GetValues(targetType).Cast<Enum>();
        return values.FirstOrDefault(x => x.GetDescription().Equals(value)) ?? value;
    }
}