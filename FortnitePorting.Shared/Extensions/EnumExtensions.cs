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
    
    public static bool IsDisabled(this Enum value)
    {
        return value.GetType().GetField(value.ToString())?.GetCustomAttributes(typeof(DisabledAttribute), false).SingleOrDefault() is not null;
    }
}

public class EnumToItemsSource(Type type) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var values = Enum.GetValues(type).Cast<Enum>();
        return values.Select(value => new EnumRecord(type, value, value.GetDescription(), value.IsDisabled())).ToList();
    }
}

public record EnumRecord(Type EnumType, Enum Value, string Description, bool IsDisabled = false)
{
    public override string ToString()
    {
        return Description;
    }
}

public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        return enumValue.GetDescription();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


public class EnumToRecordConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        return new EnumRecord(enumValue.GetType(), enumValue, enumValue.GetDescription(), enumValue.IsDisabled());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as EnumRecord;
        return enumValue.Value;
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

public class EnumGreaterOrEqualConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        var compareValue = parameter as Enum;
        return enumValue.CompareTo(compareValue) >= 0;
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

public class DisabledAttribute : Attribute
{
    
}