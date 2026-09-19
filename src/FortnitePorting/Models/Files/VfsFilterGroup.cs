using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Files;

public partial class VfsFilterGroup(string title) : ObservableObject
{
    public string Title { get; } = title;

    [ObservableProperty] private ObservableCollection<VfsFilterItem> _items = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Header))] private int _selectedCount;

    public string Header => SelectedCount > 0
        ? $"{Title} ({SelectedCount})"
        : Title;
}
