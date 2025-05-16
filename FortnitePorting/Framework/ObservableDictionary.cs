using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Framework;

public class ObservableDictionary<TKey, TValue> : ObservableCollection<ObservableKeyValuePair<TKey, TValue>>
{
    public ObservableDictionary()
    {
        
    }
    
    public ObservableDictionary(Dictionary<TKey, TValue> dict)
    {
        foreach (var (key, value) in dict)
        {
            AddOrUpdate(key, value);
        }
    }
    
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

    public void UpdateIfContains(TKey key, Action<TValue> valueModifier)
    {
        if (!TryGetValue(key, out var value)) return;
        valueModifier.Invoke(value);
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

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (ContainsKey(key))
        {
            value = GetValue(key);
            return true;
        }
        
        value = default;
        return false;
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
