using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace FortnitePorting.Shared.Extensions;

public static partial class StringExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string TitleCase(this string text)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string UnrealCase(this string text)
    {
        return UnrealCaseRegex().Replace(text, " $0");
    }

    [GeneratedRegex(@"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))")]
    private static partial Regex UnrealCaseRegex();
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