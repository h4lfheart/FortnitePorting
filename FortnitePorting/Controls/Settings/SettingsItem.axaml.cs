using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Material.Icons;

namespace FortnitePorting.Controls.Settings;

public partial class SettingsItem : UserControl
{
    [AvaDirectProperty] private string _title;
    [AvaDirectProperty] private string _description;
    
    [AvaDirectProperty] private Control? _header;
    [AvaDirectProperty] private Control? _content;
    [AvaDirectProperty] private Control? _footer;
    
    public SettingsItemModel Model { get; } = new();
    
    public SettingsItem()
    {
        InitializeComponent();
    }
}

public partial class SettingsItemModel : ObservableObject
{
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ExpandButtonIcon))] private bool _isExpanded;
    
    public MaterialIconKind ExpandButtonIcon => IsExpanded ? MaterialIconKind.ChevronUp : MaterialIconKind.ChevronDown;
}