using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FortnitePorting.Views.Extensions;

public static class MiscExtensions
{
    public static bool MoveToEnd<T>(this List<T> list, Func<T, bool> predicate)
    {
        var found = list.FirstOrDefault(predicate);
        if (found is null) return false;

        var removed = list.Remove(found);
        if (!removed) return false;
        list.Add(found);

        return true;
    }
    
    public static byte[] ToBytes(this Stream str)
    {
        var bytes = new BinaryReader(str).ReadBytes((int) str.Length);
        return bytes;
    }
    
    public static string TitleCase(this string text)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text);
    }
    
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}