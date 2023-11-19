using System.Collections.Generic;
using CUE4Parse.Utils;

namespace FortnitePorting.Extensions;

public class SimpleIni
{
    public Dictionary<string, string> this[string id] => Sections[id];
    private readonly Dictionary<string, Dictionary<string, string>> Sections = new();

    public SimpleIni(string data)
    {
        var lines = data.Split("\n");

        var currentSection = string.Empty;
        foreach (var line in lines)
            if (line.Contains('['))
            {
                var sectionName = line.SubstringAfter("[").SubstringBefore("]").Trim();
                Sections.Add(sectionName, new Dictionary<string, string>());
                currentSection = sectionName;
            }
            else if (!line.Contains('[') && line.Contains('='))
            {
                var pair = line.Split("=");
                var key = pair[0];
                var value = pair[1].Trim();
                Sections[currentSection].Add(key, value);
            }
    }
}