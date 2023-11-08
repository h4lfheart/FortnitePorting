using System.Collections.Generic;
using System.IO;
using CUE4Parse.Utils;

namespace FortnitePorting.Bundles;

public static class IniReader // Built specifically for FortniteGame/Config/Windows/CosmeticBundleMapping.ini and Cloud/BuildInfo.ini
{
    public static Ini Read(FileInfo file)
    {
        return Read(File.ReadAllText(file.FullName));
    }

    public static Ini Read(string data)
    {
        var lines = data.Split("\n");

        var currentSection = string.Empty;
        var iniData = new Ini();
        foreach (var line in lines)
        {
            if (line.Contains('['))
            {
                var sectionName = line.SubstringAfter("[").SubstringBefore("]").Trim();
                iniData.Sections.Add(sectionName, new List<IniData>());
                currentSection = sectionName;
            }
            else if (!line.Contains('[') && line.Contains('='))
            {
                var pair = line.Split("=");
                var key = pair[0];
                var value = pair[1].Trim();
                iniData.Sections[currentSection].Add(new IniData(key, value));
            }
        }

        return iniData;
    }
}

public class Ini
{
    public Dictionary<string, List<IniData>> Sections = new();
}

public class IniData
{
    public string Name;
    public string Value;

    public IniData(string name, string value)
    {
        Name = name;
        Value = value;
    }
}