using System.Globalization;
using Avalonia.Data.Converters;

namespace FortnitePorting.Shared.Extensions;

public static class StringExtensions
{
    public static string TitleCase(this string text)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text);
    }
}

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