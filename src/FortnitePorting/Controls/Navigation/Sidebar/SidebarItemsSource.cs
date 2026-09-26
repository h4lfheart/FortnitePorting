using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public class SidebarItemsSource : AvaloniaObject, ISidebarItem
{
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<SidebarItemsSource, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<SidebarItemsSource, IDataTemplate?>(nameof(ItemTemplate));

    public static readonly StyledProperty<bool> CanReorderProperty =
        AvaloniaProperty.Register<SidebarItemsSource, bool>(nameof(CanReorder), defaultValue: false);

    [Content]
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public bool CanReorder
    {
        get => GetValue(CanReorderProperty);
        set => SetValue(CanReorderProperty, value);
    }

    public event EventHandler? ItemsChanged;

    static SidebarItemsSource()
    {
        ItemsSourceProperty.Changed.AddClassHandler<SidebarItemsSource>((x, e) => x.OnItemsSourceChanged(e));
        ItemTemplateProperty.Changed.AddClassHandler<SidebarItemsSource>((x, e) => x.OnItemTemplateChanged(e));
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnCollectionChanged;
        }

        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnItemTemplateChanged(AvaloniaPropertyChangedEventArgs e)
    {
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ItemsChanged?.Invoke(this, EventArgs.Empty);
    }
}