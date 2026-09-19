namespace FortnitePorting.Shared.Extensions;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public string CommaJoin(bool includeAnd = true)
        {
            var list = enumerable.ToList();
            var joiner = includeAnd ? list.Count == 2 ? " and " : ", and " : ", ";
            return list.Count > 1
                ? string.Join(", ", list.Take(list.Count - 1)) + joiner + list.Last()
                : list.First().ToString();
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
}
