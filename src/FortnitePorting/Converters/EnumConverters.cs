using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FortnitePorting.Extensions;

namespace FortnitePorting.Converters;


public class EnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        return enumValue.Description;
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
        return new EnumRecord(enumValue.GetType(), enumValue, enumValue.Description, enumValue.IsDisabled, enumValue.Icon);
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

public class EnumNotEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumValue = value as Enum;
        var compareValue = parameter as Enum;

        return !enumValue.Equals(compareValue);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}