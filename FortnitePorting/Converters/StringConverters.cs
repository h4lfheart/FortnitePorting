using System;
using System.Globalization;
using Avalonia.Data.Converters;
using FortnitePorting.Extensions;

namespace FortnitePorting.Converters;


public class TitleCaseStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = (string) value!;
        return str.TitleCase();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class UnrealCaseStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = (string) value!;
        return str.UnrealCase();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}