using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace FortnitePorting.Models;

public class SuppressibleObservableCollection<T> : ObservableCollection<T>
{
    private bool Suppress;

    public void SetSuppression(bool state)
    {
        Suppress = state;
    }

    public void AddSuppressed(T item)
    {
        SetSuppression(true);
        Add(item);
        SetSuppression(false);
    }

    public void AddRange(IEnumerable<T>? list)
    {
        if (list is null) return;

        SetSuppression(true);

        foreach (var item in list) Add(item);

        SetSuppression(false);
        InvokeOnCollectionChanged();
    }

    public void AddRange(T[]? list)
    {
        if (list is null) return;

        SetSuppression(true);

        foreach (var item in list) Add(item);

        SetSuppression(false);
        InvokeOnCollectionChanged();
    }

    public void InvokeOnCollectionChanged(NotifyCollectionChangedAction changedAction = NotifyCollectionChangedAction.Reset)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(changedAction));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!Suppress) base.OnCollectionChanged(e);
    }

    public void Sort<TKey>(Func<T, TKey> keySelector)
    {
        InternalSort(Items.OrderBy(keySelector));
    }

    public void SortDescending<TKey>(Func<T, TKey> keySelector)
    {
        InternalSort(Items.OrderByDescending(keySelector));
    }

    public void Sort<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
    {
        InternalSort(Items.OrderBy(keySelector, comparer));
    }

    private void InternalSort(IEnumerable<T> sortedItems)
    {
        var sortedItemsList = sortedItems.ToList();

        foreach (var item in sortedItemsList) Move(IndexOf(item), sortedItemsList.IndexOf(item));
    }
}