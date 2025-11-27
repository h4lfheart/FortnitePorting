using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FortnitePorting.Extensions;

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
    
    [GeneratedRegex(@"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))|(?<=\D)(?=\d)|(?<=\d)(?=\D)")]
    private static partial Regex UnrealCaseRegex();
    
    public static int GetPropertiesExportIndexLine(string json, int index)
    {
        var reader = new JsonTextReader(new StringReader(json));

        var root = JToken.ReadFrom(reader);

        if (root is not JArray arr)
            return -1;

        var element = arr[index];

        IJsonLineInfo info = element;

        var lineIndex = info.LineNumber;

        return lineIndex;
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