using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FortnitePorting.Views.Controls;

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

        foreach (var item in list)
        {
            Add(item);
        }

        SetSuppression(false);
        InvokeOnCollectionChanged();
    }

    public void AddRange(T[]? list)
    {
        if (list is null) return;

        SetSuppression(true);

        foreach (var item in list)
        {
            Add(item);
        }

        SetSuppression(false);
        InvokeOnCollectionChanged();
    }

    public void InvokeOnCollectionChanged(NotifyCollectionChangedAction changedAction = NotifyCollectionChangedAction.Reset)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(changedAction));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!Suppress)
        {
            base.OnCollectionChanged(e);
        }
    }
}