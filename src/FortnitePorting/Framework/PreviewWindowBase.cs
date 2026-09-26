using System;
using System.Collections.ObjectModel;

namespace FortnitePorting.Framework;

public abstract class PreviewWindowBase<TWindow, TModel> : WindowBase<TModel>, IPreviewWindow
    where TWindow : PreviewWindowBase<TWindow, TModel>
    where TModel : WindowModelBase
{
    protected PreviewWindowBase(TModel? templateWindowModel = null, bool initializeWindowModel = true)
        : base(templateWindowModel, initializeWindowModel)
    {
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    protected void RemoveTabAndCloseIfEmpty<TItem>(ObservableCollection<TItem> items, object? item)
    {
        if (item is not TItem typed) return;

        items.Remove(typed);

        if (items.Count == 0)
        {
            Close();
        }
    }
}
