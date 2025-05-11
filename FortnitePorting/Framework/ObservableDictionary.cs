using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Framework;

public class ObservableDictionary<TKey, TValue> : ObservableCollection<ObservableKeyValuePair<TKey, TValue>>
{
    public void AddOrUpdate(TKey key, TValue value)
    {
        if (this.FirstOrDefault(kvp => kvp.Key.Equals(key)) is { } existing)
        {
            existing.Value = value;
        }
        else
        {
            Add(new ObservableKeyValuePair<TKey, TValue>(key, value));
        }
    }

    public bool Remove(TKey key)
    {
        if (this.FirstOrDefault(kvp => kvp.Key.Equals(key)) is { } existing)
        {
            Remove(existing);
            return true;
        }

        return false;
    }

    public TValue GetValue(TKey key)
    {
        return this.FirstOrDefault(kvp => kvp.Key.Equals(key)).Value;
    }
    
    public bool ContainsKey(TKey key)
    {
        return this.Any(kvp => kvp.Key.Equals(key));
    }

    public TValue this[TKey key]
    {
        get => GetValue(key);
        set => AddOrUpdate(key, value);
    }

    public new void Clear()
    {
        base.Clear();
    }
}

public partial class ObservableKeyValuePair<TKey, TValue>(TKey key, TValue value) : ObservableObject
{
    [ObservableProperty] private TKey _key = key;
    [ObservableProperty] private TValue _value = value;

}
