using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Data.Converters;

namespace FortnitePorting.Shared.Extensions;

public static class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TitleCase(this string text)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
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