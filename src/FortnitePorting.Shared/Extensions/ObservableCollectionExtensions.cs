using System.Collections.ObjectModel;
using DynamicData.Binding;

namespace FortnitePorting.Shared.Extensions;

public static class ObservableCollectionExtensions
{
    extension<T>(ObservableCollection<T> list)
    {
        public bool AddUnique(T item)
        {
            if (list.Contains(item)) return false;
            list.Add(item);
            return true;
        }

        public void InsertSorted(T item, SortExpressionComparer<T> comparer)
        {
            var i = list.Count;
            while (i > 0 && comparer.Compare(list[i - 1], item) > 0)
                i--;
            list.Insert(i, item);
        }
    }

    extension<T>(ObservableCollection<T> list) where T : class
    {
        public void Diff(IList<T> target)
        {
            for (var i = 0; i < target.Count; i++)
            {
                if (i < list.Count)
                {
                    if (!ReferenceEquals(list[i], target[i]))
                        list[i] = target[i];
                }
                else
                {
                    list.Add(target[i]);
                }
            }

            while (list.Count > target.Count)
                list.RemoveAt(list.Count - 1);
        }
    }
}
