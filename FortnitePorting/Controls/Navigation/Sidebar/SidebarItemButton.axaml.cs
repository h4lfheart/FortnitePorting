using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Material.Icons;

namespace FortnitePorting.Controls.Navigation.Sidebar;

public partial class SidebarItemButton : UserControl, ISidebarItem
{
    [AvaDirectProperty] private string _text;
    [AvaDirectProperty] private MaterialIconKind? _icon;
    [AvaDirectProperty] private Bitmap? _iconBitmap;

    [AvaDirectProperty] private Control? _footer;
    
    [AvaStyledProperty] private bool _isSelected = false;

    public bool UseIconBitmap => IconBitmap is not null;

    public bool ShouldShowIcon => IconBitmap is not null || Icon is not null;

    public SidebarItemButton()
    {
        InitializeComponent();
    }
    
    public SidebarItemButton(string text = "", MaterialIconKind icon = MaterialIconKind.Palette, Bitmap? iconBitmap = null, object? tag = null) : this()
    {
        Text = text;
        Icon = icon;
        IconBitmap = iconBitmap;
        Tag = tag;
    }
}