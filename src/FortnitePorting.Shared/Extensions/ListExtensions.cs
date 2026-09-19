using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace FortnitePorting.Shared.Extensions;

public static class ListExtensions
{
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
