using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DynamicData.Binding;

namespace FortnitePorting.Shared.Extensions;

public static class MiscExtensions
{
    public static bool Filter(string input, string filter)
    {
        var filters = filter.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    public static bool FilterAll(string input, IEnumerable<string> filters)
    {
        return filters.All(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    public static bool FilterAny(string input, IEnumerable<string> filters)
    {
        return filters.Any(x => input.Contains(x, StringComparison.OrdinalIgnoreCase));
    }
    
    extension<T>(List<T> list)
    {
        public void Shuffle()
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = System.Random.Shared.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void InsertMany(int index, T item, int count)
        {
            var repeat = FastRepeat<T>.Instance;
            repeat.Count = count;
            repeat.Item = item;
            list.InsertRange(index, FastRepeat<T>.Instance);
            repeat.Item = default;
        }

        public bool AddUnique(T item)
        {
            if (list.Contains(item)) return false;
            list.Add(item);
            return true;
        }
        
        public bool AddIfNotNull(T? obj)
        {
            if (obj is null) return false;
            list.Add(obj);
            return true;
        }

        public void AddRangeIfNotNull(IEnumerable<T?>? items)
        {
            if (items is null) return;
        
            foreach (var item in items)
            {
                if (item is null) continue;
                list.Add(item);
            }
        }
    }

    extension<T>(ObservableCollection<T> list)
    {
        public bool AddUnique(T item)
        {
            if (list.Contains(item)) return false;
            list.Add(item);
            return true;
        }
    }

    extension<T, K>(IDictionary<T, K> dict)
    {
        public bool AddUnique(T key, K value)
        {
            if (dict.ContainsKey(key)) return false;
            dict.Add(key, value);
            return true;
        }

        public bool AddUnique(KeyValuePair<T, K> kvp)
        {
            if (dict.ContainsKey(kvp.Key)) return false;
            dict.Add(kvp.Key, kvp.Value);
            return true;
        }
    }

    extension<T>(IEnumerable<T> enumerable)
    {
        public string CommaJoin(bool includeAnd = true)
        {
            var list = enumerable.ToList();
            var joiner = includeAnd ? list.Count == 2 ? " and " : ", and " : ", ";
            return list.Count > 1 ? string.Join(", ", list.Take(list.Count - 1)) + joiner + list.Last() : list.First().ToString();
        }

        public IEnumerable<(int index, T value)> Enumerate()
        {
            return enumerable.Select((i, val) => (val, i));
        }
        
        public IEnumerable<T> RemoveNull()
        {
            return enumerable.Where(x => x is not null);
        }

        public void ForEach(Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }
        
        public T? Random()
        {
            var list = enumerable.ToList();
            if (list.Count == 0) return default;
        
            var index = System.Random.Shared.Next(0, list.Count);
            return list[index];
        }

        public IEnumerable<T> Random(int count)
        {
            return enumerable.OrderBy(_ => System.Random.Shared.Next()).Take(count);
        }

        public int IndexOf(Predicate<T> predicate)
        {
            var array = enumerable.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                if (predicate(array[i])) return i;
            }

            return -1;
        }
    }
    
    extension(Stream stream)
    {
        public byte[] ReadToEnd()
        {
            if (stream.CanSeek)
                stream.Position = 0;
            var bytes = new BinaryReader(stream).ReadBytes((int) stream.Length);
            return bytes;
        }
        
        public  string GetHash()
        {
            return BitConverter.ToString(SHA256.HashData(stream.ReadToEnd())).Replace("-", string.Empty);
        }
    }

    extension(string text)
    {
        public byte[] StringToBytes()
        {
            return Encoding.UTF8.GetBytes(text);
        }
        
        
        public string GetHash()
        {
            return BitConverter.ToString(SHA256.HashData(File.ReadAllBytes(text))).Replace("-", string.Empty);
        }

        public T CreateXaml<T>(dynamic bindings) where T : Control
        {
            var content = AvaloniaRuntimeXamlLoader.Parse<T>(text);
            content.DataContext = bindings;
            return content;
        }
    }

    extension(byte[] data)
    {
        public string BytesToString()
        {
            return Encoding.UTF8.GetString(data);
        }
    }
    
    
    extension(FileInfo fileInfo)
    {
        public string GetHash()
        {
            return GetHash(fileInfo.FullName);
        }
        
        public string GetFileHashMD5()
        {
            using var stream = fileInfo.OpenRead();
            using var sha = MD5.Create();
            var hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLower();
        }
    }
    
    public static bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    extension<T>(IList<T> list)
    {
        public int RemoveAll(Predicate<T> predicate)
        {
            var removed = 0;
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }

        public void InsertSorted(T item, SortExpressionComparer<T> comparer) 
        {
            list.Add(item);
            var i = list.Count-1;
            for ( ; i > 0 && comparer.Compare(list[i-1], item) < 0 ; i--) {
                list[i] = list[i-1];
            }
            list[i] = item;
        }
    }

    extension<T>(Lazy<T> lazy)
    {
        public T CreateValue()
        {
            return lazy.Value;
        }
    }

    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        Copy(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
    }

    public static void Copy(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        foreach (var subDirectory in source.GetDirectories())
        {
            Copy(subDirectory, target.CreateSubdirectory(subDirectory.Name));
        }
    }

    public static bool IsProcessRunning(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.Any(process => process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
    }
    
    public static Process? GetRunningProcess(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.FirstOrDefault(process => process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
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