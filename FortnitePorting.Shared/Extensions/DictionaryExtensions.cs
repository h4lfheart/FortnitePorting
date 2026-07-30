namespace FortnitePorting.Shared.Extensions;

public static class DictionaryExtensions
{
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
}
