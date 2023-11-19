using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FortnitePorting.Extensions;

public static class MiscExtensions
{
    public static string TitleCase(this string text)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text);
    }

    public static bool Filter(string input, string filter)
    {
        var filters = filter.Trim().Split(' ');
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    public static void InsertMany<T>(this List<T> list, int index, T item, int count)
    {
        var repeat = FastRepeat<T>.Instance;
        repeat.Count = count;
        repeat.Item = item;
        list.InsertRange(index, FastRepeat<T>.Instance);
        repeat.Item = default;
    }

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

    public static bool AddUnique<T, K>(this IDictionary<T, K> dict, T key, K value)
    {
        if (dict.ContainsKey(key)) return false;
        dict.Add(key, value);
        return true;
    }

    public static bool AddUnique<T, K>(this IDictionary<T, K> dict, KeyValuePair<T, K> kvp)
    {
        if (dict.ContainsKey(kvp.Key)) return false;
        dict.Add(kvp.Key, kvp.Value);
        return true;
    }

    public static string CommaJoin<T>(this IEnumerable<T> enumerable, bool includeAnd = true)
    {
        var list = enumerable.ToList();
        var joiner = includeAnd ? list.Count == 2 ? " and " : ", and " : ", ";
        return list.Count > 1 ? string.Join(", ", list.Take(list.Count - 1)) + joiner + list.Last() : list.First().ToString();
    }

    public static byte[] ReadToEnd(this Stream str)
    {
        var bytes = new BinaryReader(str).ReadBytes((int) str.Length);
        return bytes;
    }

    public static byte[] StringToBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    public static string BytesToString(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Select((i, val) => (val, i));
    }

    public static bool AddIfNotNull<T>(this List<T> list, T? obj)
    {
        if (obj is null) return false;
        list.Add(obj);
        return true;
    }

    public static IEnumerable<T> RemoveNull<T>(this IEnumerable<T> enumerable)
    {
        return enumerable.Where(x => x is not null);
    }

    public static string GetHash(this Stream stream)
    {
        return BitConverter.ToString(SHA256.HashData(stream)).Replace("-", string.Empty);
    }
    
    public static string GetHash(this FileInfo fileInfo)
    {
        using var fileStream = fileInfo.OpenRead();
        return BitConverter.ToString(SHA256.HashData(fileStream)).Replace("-", string.Empty);
    }
}

file class FastRepeat<T> : ICollection<T>
{
    public static readonly FastRepeat<T> Instance = new();
    public int Count { get; set; }
    public bool IsReadOnly => true;
    [AllowNull] public T Item { get; set; }

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public IEnumerator<T> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        var end = arrayIndex + Count;

        for (var i = arrayIndex; i < end; ++i) array[i] = Item;
    }
}