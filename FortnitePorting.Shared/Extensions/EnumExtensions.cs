using System.ComponentModel;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Shared.Extensions;

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

public class EnumHasFlagConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        var compareValue = parameter as Enum;

        return enumValue.HasFlag(compareValue);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        var compareValue = parameter as Enum;

        return enumValue.Equals(compareValue);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}