using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

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

    public static string CommaJoin<T>(this IEnumerable<T> enumerable, bool includeAnd = true)
    {
        var list = enumerable.ToList();
        var joiner = includeAnd ? (list.Count == 2 ? " and " : ", and ") : ", ";
        return list.Count > 1 ? string.Join(", ", list.Take(list.Count - 1)) + joiner + list.Last() : list.First().ToString();
    }

    public static T? Random<T>(this IEnumerable<T> enumerable)
    {
        var list = enumerable.ToList();
        if (list.Count == 0) return default;

        var index = App.RandomGenerator.Next(0, list.Count);
        return list[index];
    }

    public static void FillDefault<T>(this List<T> enumerable, int count) where T : new()
    {
        for (var i = 0; i < count; i++)
        {
            enumerable.Add(new T());
        }
    }

    public static List<T> FillDefault<T>(int count) where T : new()
    {
        var list = new List<T>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new T());
        }

        return list;
    }

    public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> enumerable) => enumerable.Select((i, val) => (val, i));

    public static bool AddUnique<T>(this List<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }

    public static bool AddUnique<T>(this ObservableCollection<T> list, T item)
    {
        if (list.Contains(item)) return false;
        list.Add(item);
        return true;
    }

    public static double Truncate(this double value, int decimals)
    {
        var factor = Math.Pow(10, decimals);
        var result = Math.Truncate(factor * value) / factor;
        return result;
    }

    public static string AsString(this byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}