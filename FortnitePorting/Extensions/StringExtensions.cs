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
    extension(string text)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string TitleCase()
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string UnrealCase()
        {
            return UnrealCaseRegex().Replace(text, " $0");
        }
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