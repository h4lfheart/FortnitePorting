using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Material.Icons;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public partial class SidebarItemGroup : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _title = string.Empty;
    [AvaDirectProperty] private bool _isExpanded = true;
    [AvaDirectProperty] private MaterialIconKind _chevronIcon = MaterialIconKind.ChevronDown;

    public ObservableCollection<ISidebarItem> Items { get; } = [];

    public SidebarItemGroup()
    {
        InitializeComponent();
        this.GetObservable(IsExpandedProperty).Subscribe(expanded =>
            ChevronIcon = expanded ? MaterialIconKind.ChevronDown : MaterialIconKind.ChevronRight);
    }

    public SidebarItemGroup(string title, bool isExpanded = true) : this()
    {
        Title = title;
        IsExpanded = isExpanded;
    }

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;
}